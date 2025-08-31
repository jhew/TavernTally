namespace TavernTally.App
{
    public class BgState
    {
        public bool InBattlegrounds { get; private set; }
        public int HandCount  { get; private set; }
        public int BoardCount { get; private set; }
        public int ShopCount  { get; private set; }

        public void Reset()
        {
            InBattlegrounds = false;
            HandCount = BoardCount = ShopCount = 0;
        }

        public void SetMode(bool inBg)  => InBattlegrounds = inBg;
        public void SetHand(int n)      => HandCount  = Clamp(n, 0, 10);
        public void SetBoard(int n)     => BoardCount = Clamp(n, 0, 7);
        public void SetShop(int n)      => ShopCount  = Clamp(n, 0, 7);

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
