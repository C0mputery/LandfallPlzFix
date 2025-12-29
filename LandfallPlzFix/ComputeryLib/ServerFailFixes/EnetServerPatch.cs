using System;
using ComputeryLib.Utilities;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.ServerFailFixes;

public class ServerFailPatches {
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(EnetServer), nameof(EnetServer.StartServer))]
    [HarmonyPatch(typeof(UnityTransportServer), nameof(UnityTransportServer.StartServer))]
    [HarmonyPatch(typeof(JobifiedUnityTransportServer), nameof(JobifiedUnityTransportServer.StartServer))] // this one is never used to my knowlage
    private static Exception StartServerFinalizer(Exception? __exception) {
        if (__exception == null) { return null!; }
        TerminateUtility.TerminateServer("Server failed to start properly. Terminating connection.");
        return __exception;
    }
    
    /// <summary>
    /// Actually protect from malformed packets causing massive lag spikes and shit
    /// AND mAKE SURE THE PACKETS GET DISCARDED SO NO MEM LEAKS OMG LANDFALL PLEASE
    /// </summary>
    /// <param name="__exception"> harmony provided exception </param>
    /// <returns> null to swallow exception </returns>
    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ServerBase), nameof(ServerBase.InternalRecieve))]
    private static Exception InternalRecieveFinalizer(Exception? __exception) {
        if (__exception == null) { Plugin.Logger.LogError(__exception); }
        return null!;
    }
}