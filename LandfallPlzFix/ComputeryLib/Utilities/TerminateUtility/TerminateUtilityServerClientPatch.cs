using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.Utilities;

[HarmonyPatch(typeof(ServerClient))]
public static class TerminateUtilityServerClientPatch {
    /// <summary>
    /// Removes Landfalls hardcoded 10-second delay on termination to allowing our custom restart time to be accurate.
    /// </summary>
    /// <param name="time"> Harmony provided time parameter </param>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ServerClient.Terminate), typeof(float), typeof(string))]
    public static void AwakePrefix(ref float time) {
        time -= 10f;
        if (time < 0f) { time = 0f; }
    }
}