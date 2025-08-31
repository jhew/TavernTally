using System.Text.RegularExpressions;
using Serilog;

namespace TavernTally.App
{
    public static class LogParser
    {
        // ========== GAME MODE DETECTION ==========
        
        // Battlegrounds mode detection - enhanced patterns
        static readonly Regex ReBgEnter = new(@"(GAME_TYPE_BATTLEGROUNDS|GameType=GT_BATTLEGROUNDS|LoadingScreen\.PlayModeChanged.*BACON)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReBgModeStart = new(@"(Bacon|BattlegroundsGameplayUI)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        // Game state transitions
        static readonly Regex ReGameStart = new(@"TAG_CHANGE.*tag=STATE value=RUNNING", RegexOptions.Compiled);
        static readonly Regex ReGameEnd = new(@"TAG_CHANGE.*tag=STATE value=COMPLETE", RegexOptions.Compiled);
        static readonly Regex ReGameReset = new(@"CREATE_GAME", RegexOptions.Compiled);
        
        // ========== ZONE CHANGE DETECTION ==========
        
        // Enhanced hand tracking with entity validation
        static readonly Regex ReHandChange = new(@"ZONE_CHANGE Entity=.*\[(cardId=.*)\] zone=.*HAND.*zonePos=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReHandPlus = new(@"ZONE_CHANGE.*zone=.*->FRIENDLY HAND", RegexOptions.Compiled);
        static readonly Regex ReHandMinus = new(@"ZONE_CHANGE.*zone=FRIENDLY HAND->", RegexOptions.Compiled);
        
        // Enhanced board (play area) tracking
        static readonly Regex ReBoardChange = new(@"ZONE_CHANGE Entity=.*\[(cardId=.*)\] zone=.*PLAY.*zonePos=(\d+)", RegexOptions.Compiled);
        static readonly Regex ReBoardPlus = new(@"ZONE_CHANGE.*zone=.*->FRIENDLY PLAY", RegexOptions.Compiled);
        static readonly Regex ReBoardMinus = new(@"ZONE_CHANGE.*zone=FRIENDLY PLAY->", RegexOptions.Compiled);
        
        // ========== BATTLEGROUNDS SPECIFIC ==========
        
        // Tavern/Shop detection - more comprehensive patterns
        static readonly Regex ReShopRefresh = new(@"(TAG_CHANGE.*GOLD.*value=\d+|TavernUpgrade|BACON.*REVEAL|Network.*TavernShopUI)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReTavernUpgrade = new(@"(TavernUpgrade|BACON.*TIER)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex ReShopOffer = new(@"(BACON.*OFFER|Network.*CardReveal)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
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
                
                // Only continue with BG-specific parsing if we're in Battlegrounds
                if (!s.InBattlegrounds)
                    return;
                    
                // ===== BATTLEGROUNDS GAME STATE =====
                CheckGameStateTransitions(line, s);
                CheckZoneChanges(line, s);
                CheckShopAndTavern(line, s);
                
            }
            catch (System.Exception ex)
            {
                Log.Warning(ex, "Error parsing log line: {Line}", line);
            }
        }
        
        // ========== HELPER METHODS ==========
        
        private static void CheckGameModeTransitions(string line, BgState s)
        {
            // Detect entering Battlegrounds
            if (ReBgEnter.IsMatch(line) || ReBgModeStart.IsMatch(line))
            {
                if (!s.InBattlegrounds)
                {
                    Log.Debug("Detected Battlegrounds mode entry");
                    s.SetMode(true);
                }
            }
            
            // Detect game ending
            if (ReGameEnd.IsMatch(line))
            {
                if (s.InBattlegrounds)
                {
                    Log.Debug("Detected game end - exiting Battlegrounds mode");
                    s.SetMode(false);
                }
            }
            
            // Detect new game starting (reset state)
            if (ReGameReset.IsMatch(line))
            {
                Log.Debug("Detected new game creation - resetting state");
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
            // Enhanced hand tracking with position awareness
            var handMatch = ReHandChange.Match(line);
            if (handMatch.Success)
            {
                var cardId = handMatch.Groups[1].Value;
                var position = handMatch.Groups[2].Value;
                Log.Debug("Hand zone change: {CardId} at position {Position}", cardId, position);
            }
            
            // Fallback to simpler patterns for basic counting
            if (ReHandPlus.IsMatch(line))
            {
                var newCount = s.HandCount + 1;
                Log.Debug("Hand count increased: {OldCount} -> {NewCount}", s.HandCount, newCount);
                s.SetHand(newCount);
            }
            
            if (ReHandMinus.IsMatch(line))
            {
                var newCount = s.HandCount - 1;
                Log.Debug("Hand count decreased: {OldCount} -> {NewCount}", s.HandCount, newCount);
                s.SetHand(newCount);
            }
            
            // Enhanced board tracking with position awareness
            var boardMatch = ReBoardChange.Match(line);
            if (boardMatch.Success)
            {
                var cardId = boardMatch.Groups[1].Value;
                var position = boardMatch.Groups[2].Value;
                Log.Debug("Board zone change: {CardId} at position {Position}", cardId, position);
            }
            
            // Fallback to simpler patterns for basic counting
            if (ReBoardPlus.IsMatch(line))
            {
                var newCount = s.BoardCount + 1;
                Log.Debug("Board count increased: {OldCount} -> {NewCount}", s.BoardCount, newCount);
                s.SetBoard(newCount);
            }
            
            if (ReBoardMinus.IsMatch(line))
            {
                var newCount = s.BoardCount - 1;
                Log.Debug("Board count decreased: {OldCount} -> {NewCount}", s.BoardCount, newCount);
                s.SetBoard(newCount);
            }
        }
        
        private static void CheckShopAndTavern(string line, BgState s)
        {
            // Detect tavern upgrades
            if (ReTavernUpgrade.IsMatch(line))
            {
                Log.Debug("Tavern upgrade detected");
                // Tavern upgrades typically increase shop offerings
                var newShopCount = System.Math.Min(s.ShopCount + 1, 7);
                s.SetShop(newShopCount);
            }
            
            // Detect shop refreshes
            if (ReShopRefresh.IsMatch(line))
            {
                Log.Debug("Shop refresh detected");
                // Improved shop count logic based on tavern tier
                var newCount = CalculateShopCount(s.ShopCount);
                s.SetShop(newCount);
            }
            
            // Detect shop offerings for better counting
            if (ReShopOffer.IsMatch(line))
            {
                Log.Debug("Shop offering detected");
            }
        }
        
        private static int CalculateShopCount(int currentCount)
        {
            // More sophisticated shop count calculation
            // In Battlegrounds, shop offerings depend on tavern tier:
            // Tier 1: 3 minions, Tier 2: 4 minions, Tier 3+: 5+ minions
            if (currentCount <= 0) return 3; // Start with tier 1
            if (currentCount == 3) return 4; // Upgrade to tier 2
            if (currentCount == 4) return 5; // Upgrade to tier 3
            if (currentCount >= 5) return System.Math.Min(currentCount + 1, 7); // Cap at 7
            
            return currentCount;
        }
    }
}
