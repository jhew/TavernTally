using System;
using System.IO;
using System.Threading;
using Serilog;
using TavernTally;

namespace TavernTally.Tests
{
    /// <summary>
    /// Simple test class to validate phase detection logic
    /// Run this to test phase transitions without needing actual Hearthstone logs
    /// </summary>
    public static class PhaseDetectionTests
    {
        public static void RunTests()
        {
            Console.WriteLine("🧪 Running Phase Detection Tests...\n");

            var state = new BgState();
            state.SetMode(true); // Start in Battlegrounds mode

            // Test 1: Initial state should be recruit phase (default)
            Console.WriteLine($"Test 1 - Initial state: Recruit={state.InRecruitPhase}, Combat={!state.InRecruitPhase}");
            Assert(state.InRecruitPhase, "Initial state should be recruit phase");

            // Test 2: END_TURN should transition to combat phase
            LogParser.Apply("BLOCK_START SUB_ACTION_END_TURN", state);
            Console.WriteLine($"Test 2 - After END_TURN: Recruit={state.InRecruitPhase}, Combat={!state.InRecruitPhase}");
            Assert(!state.InRecruitPhase, "END_TURN should transition to combat phase");

            // Test 3: Shop action should transition back to recruit phase
            LogParser.Apply("BUY action detected", state);
            Console.WriteLine($"Test 3 - After BUY: Recruit={state.InRecruitPhase}, Combat={!state.InRecruitPhase}");
            Assert(state.InRecruitPhase, "BUY action should transition to recruit phase");

            // Test 4: Opponent turn start should transition to combat phase
            LogParser.Apply("BLOCK_START ACTION_PHASE opponent", state);
            Console.WriteLine($"Test 4 - After opponent turn: Recruit={state.InRecruitPhase}, Combat={!state.InRecruitPhase}");
            Assert(!state.InRecruitPhase, "Opponent turn should transition to combat phase");

            // Test 5: Our turn start should transition to recruit phase
            LogParser.Apply("BLOCK_START ACTION_PHASE player", state);
            Console.WriteLine($"Test 5 - After our turn: Recruit={state.InRecruitPhase}, Combat={!state.InRecruitPhase}");
            Assert(state.InRecruitPhase, "Our turn should transition to recruit phase");

            Console.WriteLine("\n✅ All phase detection tests passed!");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                Console.WriteLine($"❌ ASSERTION FAILED: {message}");
                throw new Exception($"Test failed: {message}");
            }
            else
            {
                Console.WriteLine($"✅ {message}");
            }
        }
    }
}
