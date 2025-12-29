using ComputeryLib.Utilities.WorldUtility;
using Landfall.Network;

namespace ComputeryLib.Utilities;

public static class TerminateUtility {
    public static readonly float RestartTime = Plugin.Config.Bind("Server", "RestartTime", 15f, "Time that it takes the server to reload when the game ends or when a it cannot start properly (in seconds)").Value;
    
    public static void TerminateServer(TerminateCause cause) {
        if (WorldUtilites.TryGetWorld(out ServerClient? world)) { world!.Terminate(RestartTime, cause); }
    }
    
    public static void TerminateServer(string reason) {
        if (WorldUtilites.TryGetWorld(out ServerClient? world)) { world!.Terminate(RestartTime, reason); }
    }
}