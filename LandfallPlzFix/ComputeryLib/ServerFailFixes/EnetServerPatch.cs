using System;
using ComputeryLib.ConfigImprovements;
using ComputeryLib.Utilities;
using ComputeryLib.Utilities.WorldUtility;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.ServerFailFixes;

[HarmonyPatch(typeof(ServerBase))]
public class ServerBasePatch {
    [HarmonyFinalizer]
    [HarmonyPatch(nameof(ServerBase.StartServer))]
    private static Exception StartServerFinalizer(Exception? __exception) {
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Plugin.Logger.LogError($"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

        if (__exception == null) { return null!; }
        TerminateUtility.TerminateServer("Server failed to start properly. Terminating connection.");
        return __exception;
    }
    
    /// <summary>
    /// Actually protect from malformed packets causing massive lag spikes and shit
    /// </summary>
    /// <param name="__exception"> harmony provided exception </param>
    /// <returns> null to swallow exception </returns>
    [HarmonyFinalizer]
    [HarmonyPatch(nameof(ServerBase.InternalRecieve))]
    private static Exception InternalRecieveFinalizer(Exception? __exception) {
        if (__exception == null) { Plugin.Logger.LogError(__exception); }
        return null!;
    }
}