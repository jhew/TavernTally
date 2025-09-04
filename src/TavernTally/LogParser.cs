using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Serilog;

namespace TavernTally
{
    public static class LogParser
    {
        // Reliable Battlegrounds detection patterns
        static readonly Regex ReBattlegroundsCard = new(@"cardId=BG\d+_", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReBattlegroundsGameType = new(@"GT_BATTLEGROUNDS", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Phase detection patterns based on Hearthstone log structure
        static readonly Regex ReGameStart = new(@"GameState\.Start\(\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReTurnStart = new(@"BLOCK_START.*ACTION_PHASE", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReEndTurn = new(@"BLOCK_START.*SUB_ACTION_END_TURN", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReRecruitActions = new(@"BUY|REFRESH|GOLD_LOCKED|ROLL", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Dynamic card counting patterns
        static readonly Regex ReZoneChange = new(@"TAG_CHANGE Entity=\[.*id=(\d+).*\] tag=ZONE value=(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReFullEntity = new(@"FULL_ENTITY.*id=(\d+).*zone=(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Track entities in different zones
        static readonly HashSet<int> HandEntities = new();
        static readonly HashSet<int> BoardEntities = new();
        static readonly HashSet<int> ShopEntities = new();
        
        public static void Apply(string line, BgState s)
        {
            // Early return for empty lines
            if (string.IsNullOrWhiteSpace(line))
                return;
                
            // Auto-reset if we've been in false Battlegrounds mode too long
            if (s.ShouldAutoReset())
            {
                Log.Information("🔄 AUTO-RESET - Clearing stale Battlegrounds state after 30s of inactivity");
                s.Reset();
                ResetEntityTracking();
                return;
            }
                
            try
            {
                // STRICTER Battlegrounds detection - require BG cards in PLAY zone (actual gameplay)
                if (!s.InBattlegrounds && ReBattlegroundsCard.IsMatch(line) && line.Contains("zone=PLAY"))
                {
                    Log.Information("🎯 BATTLEGROUNDS CONFIRMED! BG Card in PLAY zone detected");
                    s.SetMode(true);
                    s.SetRecruitPhase(true);
                    s.UpdateBattlegroundsActivity();
                    ResetEntityTracking();
                    return;
                }
                
                // Exit Battlegrounds mode if we see regular Hearthstone gameplay
                if (s.InBattlegrounds && (line.Contains("MULLIGAN") || line.Contains("MAIN_READY") || line.Contains("GameType=GT_RANKED")))
                {
                    Log.Information("🚪 EXITING BATTLEGROUNDS - Regular Hearthstone detected");
                    s.Reset();
                    ResetEntityTracking();
                    return;
                }
                
                // Look for GameType (but only if we also see active gameplay)
                if (!s.InBattlegrounds && ReBattlegroundsGameType.IsMatch(line))
                {
                    Log.Information("🎮 BATTLEGROUNDS GameType detected (need PLAY confirmation)");
                    // Don't immediately set BG mode - wait for PLAY zone confirmation
                    return;
                }
                
                // Only process if we're in Battlegrounds
                if (!s.InBattlegrounds)
                    return;

                // Detect phase transitions within Battlegrounds
                // Priority 1: Combat phase detection (highest priority)
                if (ReEndTurn.IsMatch(line) && s.InRecruitPhase)
                {
                    Log.Information("⚔️ COMBAT PHASE DETECTED - Player ended turn (END_TURN)");
                    s.SetRecruitPhase(false);
                    return;
                }

                // Priority 2: Opponent's turn starting (while we're in recruit phase)
                if (ReTurnStart.IsMatch(line) && s.InRecruitPhase && line.Contains("opponent") || line.Contains("OPPONENT"))
                {
                    Log.Information("⚔️ COMBAT PHASE DETECTED - Opponent's turn started while in recruit phase");
                    s.SetRecruitPhase(false);
                    return;
                }

                // Priority 3: Recruit phase detection via shop actions
                if (ReRecruitActions.IsMatch(line) && !s.InRecruitPhase)
                {
                    Log.Information("🛒 RECRUIT PHASE DETECTED - Shop action: {Action}",
                        ReRecruitActions.Match(line).Value);
                    s.SetRecruitPhase(true);
                    return;
                }

                // Priority 4: New turn/round start (our turn again) - only if we're not already in recruit phase
                if (ReTurnStart.IsMatch(line) && !s.InRecruitPhase && !line.Contains("opponent") && !line.Contains("OPPONENT"))
                {
                    Log.Information("🆕 NEW TURN DETECTED - Our turn started, back to recruit phase");
                    s.SetRecruitPhase(true);
                    return;
                }

                // Fallback: If we detect TurnStart but can't determine context, log it for debugging
                if (ReTurnStart.IsMatch(line))
                {
                    Log.Debug("🔄 TurnStart detected but conditions not met - Current state: Recruit={Recruit}, BG={BG}",
                        s.InRecruitPhase, s.InBattlegrounds);
                }

                // Error recovery: If we've been in the same phase for too long, log a warning
                var timeInCurrentPhase = DateTime.Now - s.LastStateChange;
                if (timeInCurrentPhase.TotalMinutes > 5) // 5 minutes is unusually long for a phase
                {
                    Log.Warning("⚠️ PHASE STUCK WARNING - Been in {Phase} phase for {Minutes:F1} minutes. This may indicate detection issues.",
                        s.InRecruitPhase ? "recruit" : "combat", timeInCurrentPhase.TotalMinutes);

                    // Auto-recovery: If stuck for more than 10 minutes, reset to recruit phase
                    if (timeInCurrentPhase.TotalMinutes > 10)
                    {
                        Log.Warning("🚨 PHASE AUTO-RECOVERY - Resetting to recruit phase after {Minutes:F1} minutes of inactivity",
                            timeInCurrentPhase.TotalMinutes);
                        s.SetRecruitPhase(true);
                    }
                }

                // Bounds checking: Ensure counts are reasonable
                if (s.ShopCount < 0 || s.ShopCount > 10)
                {
                    Log.Warning("⚠️ INVALID SHOP COUNT: {Count} - Resetting to 0", s.ShopCount);
                    s.SetShop(0);
                }
                if (s.HandCount < 0 || s.HandCount > 10)
                {
                    Log.Warning("⚠️ INVALID HAND COUNT: {Count} - Resetting to 0", s.HandCount);
                    s.SetHand(0);
                }
                if (s.BoardCount < 0 || s.BoardCount > 7)
                {
                    Log.Warning("⚠️ INVALID BOARD COUNT: {Count} - Resetting to 0", s.BoardCount);
                    s.SetBoard(0);
                }

                // DEBUG: Log interesting lines to understand the format
                if (line.Contains("TAG_CHANGE") && (line.Contains("ZONE") || line.Contains("zone=")))
                {
                    Log.Information("🔍 ZONE LINE: {Line}", line.Length > 150 ? line.Substring(0, 150) + "..." : line);
                }
                
                // Simple approach: Look for specific Battlegrounds activities
                // Buy/sell actions that might indicate card movement
                if (line.Contains("BUY") || line.Contains("SELL") || line.Contains("REFRESH"))
                {
                    Log.Information("🛒 SHOP ACTION: {Line}", line.Length > 100 ? line.Substring(0, 100) + "..." : line);
                    // For now, just trigger a small change to test responsiveness
                    var currentShop = s.ShopCount;
                    s.SetShop(currentShop == 3 ? 4 : 3); // Toggle between 3 and 4 to test
                    return;
                }
                
                // Parse zone changes with corrected patterns
                var zoneMatch = ReZoneChange.Match(line);
                if (zoneMatch.Success)
                {
                    if (s.InBattlegrounds) s.UpdateBattlegroundsActivity(); // Update activity when processing zone changes
                    
                    var entityId = int.Parse(zoneMatch.Groups[1].Value);
                    var zone = zoneMatch.Groups[2].Value.ToUpperInvariant();
                    
                    Log.Information("📦 ZONE CHANGE: Entity {Id} to {Zone}", entityId, zone);
                    
                    // Remove from all zones first
                    HandEntities.Remove(entityId);
                    BoardEntities.Remove(entityId);
                    ShopEntities.Remove(entityId);
                    
                    // Add to appropriate zone - using actual BG zone names
                    switch (zone)
                    {
                        case "HAND":
                            HandEntities.Add(entityId);
                            Log.Information("✋ Added to hand: {Id}", entityId);
                            break;
                        case "PLAY":
                            BoardEntities.Add(entityId);
                            Log.Information("🎲 Added to board: {Id}", entityId);
                            break;
                        case "SETASIDE":
                            // In BG, SETASIDE might be used for shop
                            ShopEntities.Add(entityId);
                            Log.Information("🛒 Added to shop: {Id}", entityId);
                            break;
                    }
                    
                    UpdateCounts(s);
                    return;
                }
                
            }
            catch (System.Exception ex)
            {
                Log.Warning(ex, "Error parsing log line");
            }
        }
        
        private static void ResetEntityTracking()
        {
            HandEntities.Clear();
            BoardEntities.Clear();
            ShopEntities.Clear();
            Log.Information("🔄 Entity tracking reset for new Battlegrounds session");
        }
        
        private static void UpdateCounts(BgState s)
        {
            var handCount = HandEntities.Count;
            var boardCount = BoardEntities.Count;
            var shopCount = Math.Max(ShopEntities.Count, 3); // Minimum 3 for shop
            
            if (s.HandCount != handCount)
            {
                s.SetHand(handCount);
                Log.Information("✋ Hand count updated to {Count}", handCount);
            }
            
            if (s.BoardCount != boardCount)
            {
                s.SetBoard(boardCount);
                Log.Information("🎲 Board count updated to {Count}", boardCount);
            }
            
            if (s.ShopCount != shopCount)
            {
                s.SetShop(shopCount);
                Log.Information("🛒 Shop count updated to {Count}", shopCount);
            }
        }
    }
}
