using System;

namespace TavernTally.App
{
    public class BgState
    {
        // ========== CORE STATE ==========
        public bool InBattlegrounds { get; private set; }
        public int HandCount  { get; private set; }
        public int BoardCount { get; private set; }
        public int ShopCount  { get; private set; }
        
        // ========== ENHANCED STATE TRACKING ==========
        public int TavernTier { get; private set; } = 1;
        public int TurnNumber { get; private set; }
        public bool InCombat { get; private set; }
        public bool InRecruitPhase { get; private set; } = true;
        public DateTime LastStateChange { get; private set; } = DateTime.Now;
        
        // ========== STATE MANAGEMENT ==========
        
        public void Reset()
        {
            InBattlegrounds = false;
            HandCount = BoardCount = ShopCount = 0;
            TavernTier = 1;
            TurnNumber = 0;
            InCombat = false;
            InRecruitPhase = true;
            LastStateChange = DateTime.Now;
        }

        public void SetMode(bool inBg)  
        { 
            if (InBattlegrounds != inBg)
            {
                InBattlegrounds = inBg;
                LastStateChange = DateTime.Now;
                
                // Reset sub-states when entering/exiting Battlegrounds
                if (!inBg)
                {
                    Reset();
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
                
                // Update tavern tier based on shop count
                UpdateTavernTierFromShopCount(newCount);
            }
        }
        
        // ========== ENHANCED STATE SETTERS ==========
        
        public void SetTavernTier(int tier)
        {
            var newTier = Clamp(tier, 1, 6);
            if (TavernTier != newTier)
            {
                TavernTier = newTier;
                LastStateChange = DateTime.Now;
                
                // Update shop count to match tavern tier
                UpdateShopCountFromTavernTier(newTier);
            }
        }
        
        public void SetTurn(int turn)
        {
            var newTurn = Math.Max(0, turn);
            if (TurnNumber != newTurn)
            {
                TurnNumber = newTurn;
                LastStateChange = DateTime.Now;
            }
        }
        
        public void SetCombatPhase(bool inCombat)
        {
            if (InCombat != inCombat)
            {
                InCombat = inCombat;
                InRecruitPhase = !inCombat; // Combat and recruit phases are mutually exclusive
                LastStateChange = DateTime.Now;
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
        
        // ========== HELPER METHODS ==========
        
        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        
        private void UpdateTavernTierFromShopCount(int shopCount)
        {
            // Infer tavern tier from shop count (Battlegrounds logic)
            int inferredTier = shopCount switch
            {
                3 => 1,
                4 => 2,
                5 => 3,
                6 => 4,
                7 => 5,
                _ => TavernTier // Keep current if unclear
            };
            
            if (TavernTier != inferredTier && inferredTier >= 1 && inferredTier <= 6)
            {
                TavernTier = inferredTier;
            }
        }
        
        private void UpdateShopCountFromTavernTier(int tier)
        {
            // Update shop count based on tavern tier (Battlegrounds logic)
            int expectedShopCount = tier switch
            {
                1 => 3,
                2 => 4,
                3 => 5,
                4 => 6,
                5 => 7,
                6 => 7,
                _ => ShopCount // Keep current if unclear
            };
            
            if (ShopCount != expectedShopCount)
            {
                ShopCount = expectedShopCount;
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
        
        public override string ToString()
        {
            return $"BG:{InBattlegrounds} Hand:{HandCount} Board:{BoardCount} Shop:{ShopCount} " +
                   $"Tier:{TavernTier} Turn:{TurnNumber} Combat:{InCombat} Recruit:{InRecruitPhase}";
        }
    }
}
