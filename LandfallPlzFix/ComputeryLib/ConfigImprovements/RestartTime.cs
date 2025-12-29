using System.Collections.Generic;
using System.Reflection.Emit;
using ComputeryLib.Utilities;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.ConfigImprovements;

[HarmonyPatch(typeof(GameRoom))]
public static class GameRoomPatch {
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(GameRoom.EndMatch), typeof(TeamStanding))]
    public static IEnumerable<CodeInstruction> FixLandLog(IEnumerable<CodeInstruction> instructions) {
        CodeMatcher matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 15f));
        matcher.Set(OpCodes.Ldc_R4, TerminateUtility.RestartTime);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 15f));
        matcher.Set(OpCodes.Ldc_R4, TerminateUtility.RestartTime);
        return matcher.InstructionEnumeration();
    }
}