using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.Utilities.WorldUtility;

[HarmonyPatch(typeof(ServerClient))]
public static class ServerClientPatch {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ServerClient.Awake))]
    public static void AwakePrefix(ref ServerClient __instance) { WorldUtilites.SetWorldClient(__instance); }
}