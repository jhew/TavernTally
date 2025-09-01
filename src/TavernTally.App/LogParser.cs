using System;
using System.Text.RegularExpressions;
using Serilog;

namespace TavernTally.App
{
    public static class LogParser
    {
        // ========== GAME MODE DETECTION ==========
        
        // Battlegrounds mode detection - enhanced patterns for real Hearthstone logs
        static readonly Regex ReBgEnter = new(@"(GAME_TYPE_BATTLEGROUNDS|GameType=GT_BATTLEGROUNDS|LoadingScreen\.PlayModeChanged.*BACON|Gameplay=GT_BATTLEGROUNDS)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReBgModeStart = new(@"(Bacon|BattlegroundsGameplayUI|BATTLEGROUNDS)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // More aggressive Battlegrounds detection - look for any Battlegrounds-related content
        static readonly Regex ReBgGeneric = new(@"(battlegrounds|bacon|tavern.*tier|bob.*tavern)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Game state transitions
        static readonly Regex ReGameStart = new(@"TAG_CHANGE.*tag=STATE value=RUNNING", RegexOptions.Compiled);
        static readonly Regex ReGameEnd = new(@"TAG_CHANGE.*tag=STATE value=COMPLETE", RegexOptions.Compiled);
        static readonly Regex ReGameReset = new(@"CREATE_GAME", RegexOptions.Compiled);
        
        // ========== ZONE CHANGE DETECTION ==========
        
        // Enhanced zone tracking for accurate card counting
        static readonly Regex ReZoneChange = new(@"ZONE_CHANGE Entity=\[.*id=(\d+).*cardId=([^\]]*)\] zone=([^-\s]+)\s*->\s*([^-\s]+).*zonePos=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReEntityInHand = new(@"tag=ZONE value=HAND.*Entity=\[.*id=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReEntityInPlay = new(@"tag=ZONE value=PLAY.*Entity=\[.*id=(\d+)", RegexOptions.Compiled);
        
        // Better patterns for Battlegrounds-specific zones
        static readonly Regex ReShopCardRevealed = new(@"(HIDE_ENTITY|SHOW_ENTITY).*\[.*cardId=([^\]]*)\].*zone=SECRET", RegexOptions.Compiled);
        static readonly Regex ReTavernBoardState = new(@"TAG_CHANGE.*BACON.*MINION_IN_TAVERN.*value=(\d+)", RegexOptions.Compiled);
        
        // Full entity tracking for precise counts
        static readonly Regex ReFullEntity = new(@"FULL_ENTITY.*id=(\d+).*zone=(\w+).*cardId=([^\s]+)", RegexOptions.Compiled);
        static readonly Regex ReTagChange = new(@"TAG_CHANGE Entity=\[.*id=(\d+).*\] tag=(\w+) value=(\w+)", RegexOptions.Compiled);
        
        // ========== BATTLEGROUNDS SPECIFIC ==========
        
        // Tavern/Shop detection - more comprehensive patterns
        static readonly Regex ReShopRefresh = new(@"(TAG_CHANGE.*GOLD.*value=\d+|TavernUpgrade|BACON.*REVEAL|Network.*TavernShopUI)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReTavernUpgrade = new(@"(TavernUpgrade|BACON.*TIER)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReShopOffer = new(@"(BACON.*OFFER|Network.*CardReveal)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Battlegrounds card purchase/sale detection
        static readonly Regex ReCardPurchased = new(@"(ZONE_CHANGE.*zone=FRIENDLY_SECRET.*->.*HAND|BUY_CARD|PURCHASE)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReCardSold = new(@"(ZONE_CHANGE.*zone=.*HAND.*->.*GRAVEYARD|SELL_CARD)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReMinionsPlayed = new(@"ZONE_CHANGE.*zone=.*HAND.*->.*PLAY", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReMinionsRemoved = new(@"ZONE_CHANGE.*zone=.*PLAY.*->.*GRAVEYARD", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Gold changes (indicating shop transactions)
        static readonly Regex ReGoldChange = new(@"TAG_CHANGE.*tag=RESOURCES.*value=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReShopTransaction = new(@"(BACON.*BUY|BACON.*SELL)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Combat and turn detection
        static readonly Regex ReCombatStart = new(@"(TAG_CHANGE.*MULLIGAN_STATE.*DONE|COMBAT_START)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReTurnStart = new(@"TAG_CHANGE.*TURN.*value=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReRecruitPhase = new(@"(BACON.*RECRUIT|SHOPPING_START)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // ========== ENTITY TRACKING ==========
        
        // Track entities being created/destroyed for more accurate counting
        static readonly Regex ReEntityCreate = new(@"CREATE_GAME.*Entity=\[.*id=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReEntityDestroy = new(@"TAG_CHANGE.*Entity=\[.*id=(\d+).*tag=ZONE.*value=GRAVEYARD", RegexOptions.Compiled);
        
        // ========== MAIN PARSING LOGIC ==========
        
        public static void Apply(string line, BgState s)
        {
            // Early return for empty lines
            if (string.IsNullOrWhiteSpace(line))
                return;
                
            try
            {
                // ===== GAME MODE DETECTION =====
                CheckGameModeTransitions(line, s);
                
                // ===== PHASE DETECTION =====
                CheckPhaseTransitions(line, s);
                
                // ===== BATTLEGROUNDS TRANSACTION DETECTION =====
                // Check for shop transactions even if not yet detected as in Battlegrounds
                // This helps catch transitions and provides more responsive detection
                CheckShopTransactions(line, s);
                
                // Only continue with full BG-specific parsing if we're in Battlegrounds
                if (!s.InBattlegrounds)
                    return;
                    
                // ===== BATTLEGROUNDS GAME STATE =====
                CheckGameStateTransitions(line, s);
                CheckZoneChanges(line, s);
                CheckShopAndTavern(line, s);
                
                // Initialize default values if we're in BG but have no counts yet
                EnsureReasonableDefaults(s);
                
                // Enhanced logging for debugging
                if (line.Contains("ZONE_CHANGE") || line.Contains("TAG_CHANGE") || line.Contains("BACON"))
                {
                    Log.Information("Parsed line: {Line}", line);
                    Log.Debug("Current state: Shop={Shop}, Hand={Hand}, Board={Board}, Tier={Tier}, InBG={InBG}, InRecruit={InRecruit}", 
                        s.ShopCount, s.HandCount, s.BoardCount, s.TavernTier, s.InBattlegrounds, s.InRecruitPhase);
                }
                
            }
            catch (System.Exception ex)
            {
                Log.Warning(ex, "Error parsing log line: {Line}", line);
            }
        }
        
        private static void CheckPhaseTransitions(string line, BgState s)
        {
            // Detect recruit phase (shopping time)
            if (line.Contains("RECRUITING", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("TAVERN", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("SHOP", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("BUY", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("SELL", StringComparison.OrdinalIgnoreCase))
            {
                s.SetRecruitPhase(true);
            }
            
            // Detect combat phase
            if (line.Contains("COMBAT", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("BATTLE", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("ATTACKING", StringComparison.OrdinalIgnoreCase))
            {
                s.SetCombatPhase(true);
            }
            
            // For now, if we're in Battlegrounds but don't know the phase, assume recruit
            if (s.InBattlegrounds && !s.InRecruitPhase && !s.InCombat)
            {
                s.SetRecruitPhase(true);
            }
        }
        
        private static void EnsureReasonableDefaults(BgState s)
        {
            // Ensure we have reasonable starting values for Battlegrounds
            if (s.InBattlegrounds && s.ShopCount == 0)
            {
                // Start with tier 1 shop
                s.SetShop(GetShopCountForTier(1));
                s.SetTavernTier(1);
                Log.Debug("Applied default shop count for Battlegrounds");
            }
        }
        
        // ========== HELPER METHODS ==========
        
        private static void CheckGameModeTransitions(string line, BgState s)
        {
            // Multiple detection methods for Battlegrounds
            bool foundBattlegrounds = false;
            
            // Method 1: Explicit game type detection
            if (ReBgEnter.IsMatch(line) || ReBgModeStart.IsMatch(line))
            {
                foundBattlegrounds = true;
                Log.Information("Battlegrounds detected via explicit pattern: {Line}", line);
            }
            
            // Method 2: Generic Battlegrounds content detection
            if (!foundBattlegrounds && ReBgGeneric.IsMatch(line))
            {
                foundBattlegrounds = true;
                Log.Information("Battlegrounds detected via generic pattern: {Line}", line);
            }
            
            // Method 3: Look for specific Battlegrounds UI elements or cards
            if (!foundBattlegrounds && (line.Contains("BattlegroundsGameplayUI", StringComparison.OrdinalIgnoreCase) ||
                                       line.Contains("BACON", StringComparison.OrdinalIgnoreCase) ||
                                       line.Contains("Tavern", StringComparison.OrdinalIgnoreCase)))
            {
                foundBattlegrounds = true;
                Log.Information("Battlegrounds detected via keyword search: {Line}", line);
            }
            
            if (foundBattlegrounds && !s.InBattlegrounds)
            {
                Log.Information("Enabling Battlegrounds mode");
                s.SetMode(true);
            }
            
            // Detect game ending
            if (ReGameEnd.IsMatch(line))
            {
                if (s.InBattlegrounds)
                {
                    Log.Information("Game ended - exiting Battlegrounds mode");
                    s.SetMode(false);
                }
            }
            
            // Detect new game starting (reset state)
            if (ReGameReset.IsMatch(line))
            {
                Log.Information("New game detected - resetting state");
                s.Reset();
            }
        }
        
        private static void CheckGameStateTransitions(string line, BgState s)
        {
            // Detect game actually starting
            if (ReGameStart.IsMatch(line))
            {
                Log.Debug("Battlegrounds game started");
            }
            
            // Detect turn changes for better state tracking
            var turnMatch = ReTurnStart.Match(line);
            if (turnMatch.Success)
            {
                var turnNumber = turnMatch.Groups[1].Value;
                Log.Debug("Turn {Turn} started", turnNumber);
            }
            
            // Detect combat phases
            if (ReCombatStart.IsMatch(line))
            {
                Log.Debug("Combat phase started");
                s.SetCombatPhase(true); // Set to combat mode
            }
            
            if (ReRecruitPhase.IsMatch(line))
            {
                Log.Debug("Recruit/Shopping phase started");
                s.SetRecruitPhase(true); // Set to recruit mode
            }
        }
        
        private static void CheckZoneChanges(string line, BgState s)
        {
            // Parse comprehensive zone changes for accurate counting
            var zoneMatch = ReZoneChange.Match(line);
            if (zoneMatch.Success)
            {
                var entityId = zoneMatch.Groups[1].Value;
                var cardId = zoneMatch.Groups[2].Value;
                var fromZone = zoneMatch.Groups[3].Value;
                var toZone = zoneMatch.Groups[4].Value;
                var position = zoneMatch.Groups[5].Value;
                
                Log.Debug("Zone change: Entity {EntityId} ({CardId}) from {FromZone} to {ToZone} at position {Position}", 
                    entityId, cardId, fromZone, toZone, position);
                
                // Track cards entering/leaving friendly zones
                UpdateCardCounts(s, fromZone, toZone, cardId);
            }
            
            // Specific detection for card purchases/sales
            CheckShopTransactions(line, s);
            
            // Track full entity creation for more accurate counts
            var entityMatch = ReFullEntity.Match(line);
            if (entityMatch.Success)
            {
                var entityId = entityMatch.Groups[1].Value;
                var zone = entityMatch.Groups[2].Value;
                var cardId = entityMatch.Groups[3].Value;
                
                Log.Debug("Full entity: {EntityId} ({CardId}) in zone {Zone}", entityId, cardId, zone);
                
                // Update counts based on entity creation in zones
                if (zone.Contains("HAND", StringComparison.OrdinalIgnoreCase))
                {
                    RecalculateHandCount(s);
                }
                else if (zone.Contains("PLAY", StringComparison.OrdinalIgnoreCase))
                {
                    RecalculateBoardCount(s);
                }
            }
            
            // Track tavern-specific board state
            var tavernMatch = ReTavernBoardState.Match(line);
            if (tavernMatch.Success)
            {
                var shopCount = int.Parse(tavernMatch.Groups[1].Value);
                Log.Debug("Tavern board state: {ShopCount} minions", shopCount);
                s.SetShop(shopCount);
            }
        }
        
        private static void CheckShopTransactions(string line, BgState s)
        {
            // Enhanced card purchase detection - multiple patterns for robustness
            if (ReCardPurchased.IsMatch(line) || line.Contains("BUY_CARD") || line.Contains("PURCHASE"))
            {
                Log.Information("Card purchase detected: {Line}", line);
                s.SetHand(s.HandCount + 1);
                
                // If not in BG mode but we're seeing purchases, might be a BG game
                if (!s.InBattlegrounds)
                {
                    Log.Information("Purchase detected outside BG mode - checking if this is Battlegrounds");
                    s.SetMode(true); // Assume Battlegrounds if we see purchases
                }
            }
            
            // Enhanced card sale detection
            if (ReCardSold.IsMatch(line) || line.Contains("SELL_CARD"))
            {
                Log.Information("Card sale detected: {Line}", line);
                s.SetHand(Math.Max(0, s.HandCount - 1));
                
                // If not in BG mode but we're seeing sales, might be a BG game
                if (!s.InBattlegrounds)
                {
                    Log.Information("Sale detected outside BG mode - checking if this is Battlegrounds");
                    s.SetMode(true); // Assume Battlegrounds if we see sales
                }
            }
            
            // Enhanced minion play detection
            if (ReMinionsPlayed.IsMatch(line))
            {
                Log.Information("Minion played: {Line}", line);
                s.SetHand(Math.Max(0, s.HandCount - 1));
                s.SetBoard(s.BoardCount + 1);
            }
            
            // Detect minions being removed from board
            if (ReMinionsRemoved.IsMatch(line))
            {
                Log.Information("Minion removed from board: {Line}", line);
                s.SetBoard(Math.Max(0, s.BoardCount - 1));
            }
            
            // Detect general shop transactions
            if (ReShopTransaction.IsMatch(line))
            {
                Log.Information("Shop transaction detected: {Line}", line);
                // Trigger a state recalculation
                EnsureReasonableDefaults(s);
            }
            
            // Track gold changes as indicators of shop activity
            var goldMatch = ReGoldChange.Match(line);
            if (goldMatch.Success)
            {
                var goldAmount = goldMatch.Groups[1].Value;
                Log.Debug("Gold changed to: {Gold}", goldAmount);
                // Gold changes often accompany purchases/sales
            }
            
            // Test for specific Battlegrounds keywords that indicate activity
            if (line.Contains("BACON", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("TAVERN", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("BATTLEGROUNDS", StringComparison.OrdinalIgnoreCase))
            {
                Log.Debug("Battlegrounds-related activity detected: {Line}", line);
                
                // If we see BG activity but aren't in BG mode, enable it
                if (!s.InBattlegrounds)
                {
                    Log.Information("Battlegrounds activity detected - enabling BG mode");
                    s.SetMode(true);
                }
            }
        }
        
        private static void UpdateCardCounts(BgState s, string fromZone, string toZone, string cardId)
        {
            // Only track meaningful card movements
            if (string.IsNullOrEmpty(cardId) || cardId == "UNKNOWN")
                return;
                
            bool fromHand = fromZone.Contains("HAND", StringComparison.OrdinalIgnoreCase);
            bool toHand = toZone.Contains("HAND", StringComparison.OrdinalIgnoreCase);
            bool fromPlay = fromZone.Contains("PLAY", StringComparison.OrdinalIgnoreCase);
            bool toPlay = toZone.Contains("PLAY", StringComparison.OrdinalIgnoreCase);
            bool fromSecret = fromZone.Contains("SECRET", StringComparison.OrdinalIgnoreCase);
            bool toGraveyard = toZone.Contains("GRAVEYARD", StringComparison.OrdinalIgnoreCase);
            
            // Hand count changes
            if (toHand && !fromHand)
            {
                s.SetHand(s.HandCount + 1);
                Log.Information("Card added to hand: {CardId}, new count: {Count}", cardId, s.HandCount);
            }
            else if (fromHand && !toHand)
            {
                s.SetHand(Math.Max(0, s.HandCount - 1));
                Log.Information("Card removed from hand: {CardId}, new count: {Count}", cardId, s.HandCount);
            }
            
            // Board count changes
            if (toPlay && !fromPlay)
            {
                s.SetBoard(s.BoardCount + 1);
                Log.Information("Card added to board: {CardId}, new count: {Count}", cardId, s.BoardCount);
            }
            else if (fromPlay && !toPlay)
            {
                s.SetBoard(Math.Max(0, s.BoardCount - 1));
                Log.Information("Card removed from board: {CardId}, new count: {Count}", cardId, s.BoardCount);
            }
            
            // Special case: Cards from SECRET zone (shop) to HAND (purchase)
            if (fromSecret && toHand)
            {
                Log.Information("Card purchased from shop: {CardId}", cardId);
                // Hand count already updated above
            }
            
            // Special case: Cards from HAND to GRAVEYARD (sell)
            if (fromHand && toGraveyard)
            {
                Log.Information("Card sold: {CardId}", cardId);
                // Hand count already updated above
            }
        }
        
        private static void RecalculateHandCount(BgState s)
        {
            // This would ideally maintain a list of entities and recalculate
            // For now, we'll rely on the zone change tracking
            Log.Debug("Hand recalculation triggered, current count: {Count}", s.HandCount);
        }
        
        private static void RecalculateBoardCount(BgState s)
        {
            // This would ideally maintain a list of entities and recalculate
            // For now, we'll rely on the zone change tracking
            Log.Debug("Board recalculation triggered, current count: {Count}", s.BoardCount);
        }
        
        private static void CheckShopAndTavern(string line, BgState s)
        {
            // Detect tavern upgrades and set appropriate shop count
            if (ReTavernUpgrade.IsMatch(line))
            {
                Log.Debug("Tavern upgrade detected");
                var newTier = s.TavernTier + 1;
                s.SetTavernTier(newTier);
                
                // Set shop count based on tavern tier
                var newShopCount = GetShopCountForTier(newTier);
                s.SetShop(newShopCount);
                Log.Information("Tavern upgraded to tier {Tier}, shop now has {ShopCount} slots", newTier, newShopCount);
            }
            
            // Detect shop refreshes - maintain current tier's shop count
            if (ReShopRefresh.IsMatch(line))
            {
                Log.Debug("Shop refresh detected");
                var currentShopCount = GetShopCountForTier(s.TavernTier);
                s.SetShop(currentShopCount);
                Log.Debug("Shop refreshed, maintaining {ShopCount} slots for tier {Tier}", currentShopCount, s.TavernTier);
            }
            
            // Detect shop card reveals for better counting
            var shopCardMatch = ReShopCardRevealed.Match(line);
            if (shopCardMatch.Success)
            {
                var cardId = shopCardMatch.Groups[2].Value;
                Log.Debug("Shop card revealed: {CardId}", cardId);
                
                // Ensure shop count is at least as many as we're seeing
                var expectedCount = GetShopCountForTier(s.TavernTier);
                if (s.ShopCount < expectedCount)
                {
                    s.SetShop(expectedCount);
                    Log.Debug("Adjusted shop count to {ShopCount} based on card reveals", expectedCount);
                }
            }
            
            // Detect shop offerings for better counting
            if (ReShopOffer.IsMatch(line))
            {
                Log.Debug("Shop offering detected");
                // Ensure we have the correct shop count for current tier
                var expectedCount = GetShopCountForTier(s.TavernTier);
                if (s.ShopCount != expectedCount)
                {
                    s.SetShop(expectedCount);
                    Log.Debug("Corrected shop count to {ShopCount} for tier {Tier}", expectedCount, s.TavernTier);
                }
            }
        }
        
        private static int GetShopCountForTier(int tier)
        {
            // Battlegrounds shop slot count by tavern tier
            return tier switch
            {
                1 => 3,  // Tier 1: 3 minions
                2 => 4,  // Tier 2: 4 minions  
                3 => 5,  // Tier 3: 5 minions
                4 => 6,  // Tier 4: 6 minions
                5 => 7,  // Tier 5: 7 minions
                6 => 7,  // Tier 6: 7 minions (max)
                _ => 3   // Default to tier 1
            };
        }
    }
}
