using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.Utilities;

[HarmonyPatch(typeof(ServerClient))]
public static class WorldUtilityServerClientPatch {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ServerClient.Awake))]
    public static void AwakePrefix(ref ServerClient __instance) { WorldUtilities.SetWorldClient(__instance); }
}