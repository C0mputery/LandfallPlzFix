using System;
using ComputeryLib.Utilities;
using ENet;
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
        Plugin.Logger.LogError(__exception);
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
        if (__exception != null) { Plugin.Logger.LogError(__exception); }
        return null!;
    }

    // Flush the server and do not de-init enet.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnetServer), nameof(EnetServer.Kill))]
    public static bool KillPostfix(EnetServer __instance) {
        __instance.Host.Flush();
        __instance.Host.Dispose();
        ServerClient.m_Server = null; // We gotta remove this or it will try to reuse the disposed host.
        return false;
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ServerClient), nameof(ServerClient.OnApplicationQuit))]
    public static void OnApplicationQuitPostfix() {
        Library.Deinitialize(); // Always deinitialize ENet on application quit rather than on server kill.
    }
}