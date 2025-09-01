using System;
using TavernTally.App;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== TavernTally Log Parser Test ===");
        
        var state = new BgState();
        
        // Test the exact log lines we generated
        var testLines = new[]
        {
            "D 23:30:00.0000000 GameState.DebugPrintPower() - CREATE_GAME",
            "D 23:30:01.0000000 GameState.DebugPrintPower() - GAME_TYPE_BATTLEGROUNDS", 
            "D 23:30:02.0000000 GameState.DebugPrintPower() - TAG_CHANGE Entity=[GameEntity id=1] tag=STATE value=RUNNING",
            "D 23:30:03.0000000 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Alleycat id=25 zone=FRIENDLY_SECRET zonePos=1 cardId=CFM_315 player=2] zone from FRIENDLY_SECRET -> HAND",
            "D 23:30:04.0000000 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Murloc Tidehunter id=26 zone=FRIENDLY_SECRET zonePos=2 cardId=EX1_506 player=2] zone from FRIENDLY_SECRET -> HAND",
            "D 23:30:05.0000000 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Alleycat id=25 zone=HAND zonePos=1 cardId=CFM_315 player=2] zone from HAND -> PLAY",
            "D 23:30:06.0000000 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[Murloc Tidehunter id=26 zone=HAND zonePos=1 cardId=EX1_506 player=2] zone from HAND -> GRAVEYARD"
        };
        
        Console.WriteLine("Initial state:");
        PrintState(state);
        
        Console.WriteLine("\nProcessing log lines:");
        foreach (var line in testLines)
        {
            Console.WriteLine($"\nProcessing: {line}");
            var prevShop = state.ShopCount;
            var prevHand = state.HandCount;
            var prevBoard = state.BoardCount;
            var prevInBg = state.InBattlegrounds;
            
            LogParser.Apply(line, state);
            
            if (state.InBattlegrounds != prevInBg || state.ShopCount != prevShop || 
                state.HandCount != prevHand || state.BoardCount != prevBoard)
            {
                Console.WriteLine("*** STATE CHANGED ***");
                PrintState(state);
            }
            else
            {
                Console.WriteLine("No state change");
            }
        }
        
        Console.WriteLine("\nFinal state:");
        PrintState(state);
        
        Console.WriteLine("\nExpected final state: InBG=True, Hand=0, Board=1, Shop=3");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static void PrintState(BgState state)
    {
        Console.WriteLine($"  InBattlegrounds: {state.InBattlegrounds}");
        Console.WriteLine($"  Hand: {state.HandCount}");
        Console.WriteLine($"  Board: {state.BoardCount}");
        Console.WriteLine($"  Shop: {state.ShopCount}");
        Console.WriteLine($"  Tavern Tier: {state.TavernTier}");
    }
}
