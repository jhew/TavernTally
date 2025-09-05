using System;
using Serilog;

namespace TavernTally
{
    public class BgState
    {
        // ========== CORE STATE ==========
        public bool InBattlegrounds { get; private set; }
        public int HandCount  { get; private set; }
        public int BoardCount { get; private set; }
        public int ShopCount  { get; private set; }
        
        // ========== MANUAL OVERRIDE COUNTS ==========
        public int ManualShopCount { get; set; }
        public bool UseManualCounts { get; set; }
        
        // Effective counts (manual overrides when enabled, otherwise parsed counts)
        public int EffectiveShopCount => UseManualCounts ? ManualShopCount : ShopCount;
        public int EffectiveHandCount => UseManualCounts ? 0 : HandCount; // Show hand count when not in manual mode
        public int EffectiveBoardCount => UseManualCounts ? 0 : BoardCount; // Show board count when not in manual mode
        
        // ========== ENHANCED STATE TRACKING ==========
        public int TavernTier { get; private set; } = 1;
        public int TurnNumber { get; private set; }
        public bool InCombat { get; private set; } = false;
        public bool InRecruitPhase { get; private set; } = false; // Start as false, detect when actually in recruit phase
        public DateTime LastStateChange { get; private set; } = DateTime.Now;
        public DateTime LastBattlegroundsActivity { get; private set; } = DateTime.Now;
        
        // ========== STATE MANAGEMENT ==========
        
        public void Reset()
        {
            InBattlegrounds = false;
            HandCount = BoardCount = ShopCount = 0;
            TavernTier = 1;
            TurnNumber = 0;
            InCombat = false;
            InRecruitPhase = false; // Start as false, will be detected
            LastStateChange = DateTime.Now;
            LastBattlegroundsActivity = DateTime.Now; // Reset activity timestamp too
        }

        public void SetMode(bool inBg)  
        { 
            if (InBattlegrounds != inBg)
            {
                InBattlegrounds = inBg;
                LastStateChange = DateTime.Now;
                
                if (inBg)
                {
                    LastBattlegroundsActivity = DateTime.Now;
                    // Initialize with reasonable defaults when entering Battlegrounds
                    if (ShopCount == 0) ShopCount = 3; // Default tier 1 shop
                    if (TavernTier == 0) TavernTier = 1;
                }
            }
        }
        
        public void SetHand(int n)      
        { 
            var newCount = Clamp(n, 0, 10);
            if (HandCount != newCount)
            {
                HandCount = newCount;
                LastStateChange = DateTime.Now;
            }
        }
        
        public void SetBoard(int n)     
        { 
            var newCount = Clamp(n, 0, 7);
            if (BoardCount != newCount)
            {
                BoardCount = newCount;
                LastStateChange = DateTime.Now;
            }
        }
        
        public void SetShop(int n)      
        { 
            var newCount = Clamp(n, 0, 7);
            if (ShopCount != newCount)
            {
                ShopCount = newCount;
                LastStateChange = DateTime.Now;
                
                // Update tavern tier based on shop count if it makes sense
                UpdateTavernTierFromShopCount(ShopCount);
            }
        }
        
        public void SetRecruitPhase(bool inRecruit)
        {
            if (InRecruitPhase != inRecruit)
            {
                InRecruitPhase = inRecruit;
                InCombat = !inRecruit; // Combat and recruit phases are mutually exclusive
                LastStateChange = DateTime.Now;
            }
        }
        
        // ========== ACTIVITY TRACKING ==========
        
        public void UpdateBattlegroundsActivity()
        {
            if (InBattlegrounds)
            {
                LastBattlegroundsActivity = DateTime.Now;
            }
        }
        
        public bool ShouldAutoReset()
        {
            // Auto-reset if we've been in "Battlegrounds" mode for 10+ seconds without any BG activity
            return InBattlegrounds && 
                   (DateTime.Now - LastBattlegroundsActivity).TotalSeconds > 10;
        }
        
        // ========== HELPER METHODS ==========
        
        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        
        private void UpdateTavernTierFromShopCount(int shopCount)
        {
            // Infer tavern tier from shop count (Battlegrounds logic)
            int inferredTier = shopCount switch
            {
                3 => 1,  // Tier 1: 3 shop slots
                4 => 2,  // Tier 2: 4 shop slots
                5 => 3,  // Tier 3: 5 shop slots
                6 => 4,  // Tier 4: 6 shop slots
                7 => 5,  // Tier 5+: 7 shop slots
                _ => TavernTier // Keep current tier if shop count doesn't match expected pattern
            };

            if (inferredTier != TavernTier && inferredTier >= 1 && inferredTier <= 6)
            {
                TavernTier = inferredTier;
                Log.Information("Tavern tier updated to {Tier} based on shop count {ShopCount}", TavernTier, shopCount);
            }
        }
        
        // ========== STATE VALIDATION ==========
        
        public bool IsStateValid()
        {
            return HandCount >= 0 && HandCount <= 10 &&
                   BoardCount >= 0 && BoardCount <= 7 &&
                   ShopCount >= 0 && ShopCount <= 7 &&
                   TavernTier >= 1 && TavernTier <= 6 &&
                   TurnNumber >= 0;
        }
        
        /// <summary>
        /// Detect if we're already in a Battlegrounds match based on current state
        /// Used during initial log parsing to handle mid-match startup
        /// </summary>
        public bool DetectInitialBattlegroundsState(int bgCardCount)
        {
            // Lower threshold for initial detection - be more aggressive
            if (bgCardCount >= 2)
            {
                Log.Information("🎯 INITIAL BATTLEGROUNDS DETECTED - {Count} BG cards found, activating overlay", bgCardCount);
                SetMode(true);
                SetRecruitPhase(true); // Assume recruit phase initially
                UpdateBattlegroundsActivity();
                Log.Information("✅ Battlegrounds mode activated - Overlay should now display");
                return true;
            }
            return false;
        }
        
        public override string ToString()
        {
            return $"BG:{InBattlegrounds} Hand:{HandCount} Board:{BoardCount} Shop:{ShopCount} " +
                   $"Tier:{TavernTier} Turn:{TurnNumber} Combat:{InCombat} Recruit:{InRecruitPhase}";
        }
    }
}
