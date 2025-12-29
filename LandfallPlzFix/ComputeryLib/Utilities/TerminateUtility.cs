using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.Utilities;

public static class TerminateUtility {
    public static readonly float RestartTime = Plugin.Config.Bind("Server", "RestartTime", 25f, "Time that it takes the server to reload when the game ends or when a it cannot start properly (in seconds)").Value;
    
    public static void TerminateServer(TerminateCause cause) {
        if (WorldUtilities.TryGetWorld(out ServerClient? world)) { world!.Terminate(RestartTime, cause); }
    }
    
    public static void TerminateServer(string reason) {
        if (WorldUtilities.TryGetWorld(out ServerClient? world)) { world!.Terminate(RestartTime, reason); }
    }
    
    /// <summary>
    /// Removes Landfalls hardcoded 10-second delay on termination to allowing our custom restart time to be accurate.
    /// </summary>
    /// <param name="time"> Harmony provided time parameter </param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ServerClient), nameof(ServerClient.Terminate), typeof(float), typeof(string))]
    public static void AwakePrefix(ref float time) {
        time -= 10f;
        if (time < 0f) { time = 0f; }
    }
}