using System;
using TavernTally;

class LogParserTest
{
    static void Main()
    {
        Console.WriteLine("=== TavernTally Enhanced Log Parser Test ===\n");
        
        // Initialize logging (normally done in App startup)
        TavernTally.Logging.Init();
        
        var state = new BgState();
        
        // Test data with various Battlegrounds scenarios
        string[] testLines = {
            "D 10:30:15.1234567 GameState.DebugPrintPower() - CREATE_GAME",
            "D 10:30:16.3456789 LoadingScreen.PlayModeChanged() - BACON",
            "D 10:30:17.4567890 GameState.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=STATE value=RUNNING",
            "D 10:30:18.5678901 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[entityName=The Coin id=2 zone=DECK zonePos=0 cardId=GAME_005 player=2] zone from DECK -> FRIENDLY HAND",
            "D 10:30:19.6789012 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[entityName=Murloc Tidehunter id=3 zone=HAND zonePos=1 cardId=TB_BaconUps_061 player=2] zone from -> FRIENDLY HAND",
            "D 10:30:20.7890123 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[entityName=Righteous Protector id=4 zone=HAND zonePos=2 cardId=TB_BaconUps_062 player=2] zone from -> FRIENDLY HAND",
            "D 10:30:21.8901234 GameState.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=TURN value=1",
            "D 10:30:22.9012345 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[entityName=Murloc Tidehunter id=3 zone=HAND zonePos=1 cardId=TB_BaconUps_061 player=2] zone from FRIENDLY HAND -> FRIENDLY PLAY",
            "D 10:30:23.0123456 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[entityName=Righteous Protector id=4 zone=HAND zonePos=2 cardId=TB_BaconUps_062 player=2] zone from FRIENDLY HAND -> FRIENDLY PLAY",
            "D 10:30:24.1234567 Network.SendChoices() - TavernShopUI REFRESH",
            "D 10:30:25.2345678 GameState.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=GOLD value=5",
            "D 10:30:26.3456789 GameState.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=TURN value=2",
            "D 10:30:27.4567890 GameState.DebugPrintPower() - BACON TIER UPGRADE",
            "D 10:30:28.5678901 GameState.DebugPrintPower() - ZONE_CHANGE Entity=[entityName=Micro Mummy id=5 zone=HAND zonePos=3 cardId=TB_BaconUps_100 player=2] zone from -> FRIENDLY HAND",
            "D 10:30:29.6789012 GameState.DebugPrintPower() - COMBAT_START",
            "D 10:30:30.7890123 GameState.DebugPrintPower() - TAG_CHANGE Entity=GameEntity tag=STATE value=COMPLETE"
        };
        
        Console.WriteLine("Initial State: " + state.ToString());
        Console.WriteLine();
        
        // Process each test line
        for (int i = 0; i < testLines.Length; i++)
        {
            var line = testLines[i];
            var oldState = state.ToString();
            
            LogParser.Apply(line, state);
            
            var newState = state.ToString();
            
            if (oldState != newState)
            {
                Console.WriteLine($"Line {i+1}: {line}");
                Console.WriteLine($"State changed: {oldState} -> {newState}");
                Console.WriteLine();
            }
        }
        
        Console.WriteLine("=== Final State ===");
        Console.WriteLine($"In Battlegrounds: {state.InBattlegrounds}");
        Console.WriteLine($"Hand Count: {state.HandCount}");
        Console.WriteLine($"Board Count: {state.BoardCount}");
        Console.WriteLine($"Shop Count: {state.ShopCount}");
        Console.WriteLine($"Tavern Tier: {state.TavernTier}");
        Console.WriteLine($"Turn Number: {state.TurnNumber}");
        Console.WriteLine($"In Combat: {state.InCombat}");
        Console.WriteLine($"In Recruit Phase: {state.InRecruitPhase}");
        
        Console.WriteLine($"\nState Validation: {state.IsStateValid()}");
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
