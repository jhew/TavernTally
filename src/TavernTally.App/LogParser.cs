using System.Text.RegularExpressions;

namespace TavernTally.App
{
    public static class LogParser
    {
        // Extremely conservative, minimal patterns. Improve iteratively.
        // Detect BG mode enter/exit (heuristic: Battlegrounds strings in log)
        static readonly Regex ReBgEnter = new(@"GAME_TYPE_BATTLEGROUNDS|GameType=GT_BATTLEGROUNDS", RegexOptions.Compiled);
        static readonly Regex ReGameEnd = new(@"TAG_CHANGE.*tag=STATE value=COMPLETE", RegexOptions.Compiled);

        // Hand count: lines like "ZONE_CHANGE ... to FRIENDLY HAND" – count occurrences from your controller if needed.
        static readonly Regex ReHandPlus = new(@"ZONE.*to FRIENDLY HAND", RegexOptions.Compiled);
        static readonly Regex ReHandMinus = new(@"ZONE.*from FRIENDLY HAND", RegexOptions.Compiled);

        // Board (play area) changes
        static readonly Regex RePlayPlus = new(@"ZONE.*to FRIENDLY PLAY", RegexOptions.Compiled);
        static readonly Regex RePlayMinus = new(@"ZONE.*from FRIENDLY PLAY", RegexOptions.Compiled);

        // Shop refresh (heuristic): tavern offering lines can be inconsistent; start with a trigger word and refine.
        static readonly Regex ReShopRefresh = new(@"Battlegrounds.*Tavern|SHOP REFRESH", RegexOptions.Compiled);

        public static void Apply(string line, BgState s)
        {
            if (ReBgEnter.IsMatch(line)) s.SetMode(true);
            if (ReGameEnd.IsMatch(line)) s.SetMode(false);

            if (!s.InBattlegrounds) return;

            if (ReHandPlus.IsMatch(line)) s.SetHand(s.HandCount + 1);
            if (ReHandMinus.IsMatch(line)) s.SetHand(s.HandCount - 1);

            if (RePlayPlus.IsMatch(line)) s.SetBoard(s.BoardCount + 1);
            if (RePlayMinus.IsMatch(line)) s.SetBoard(s.BoardCount - 1);

            if (ReShopRefresh.IsMatch(line))
            {
                // simple rotation 3..7 -> refine later using exact positions
                int next = s.ShopCount <= 0 ? 3 : (s.ShopCount % 7) + 1;
                if (next < 3) next = 3;
                s.SetShop(next);
            }
        }
    }
}
