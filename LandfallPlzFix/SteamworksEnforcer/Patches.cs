using System.IO;
using BepInEx;
using HarmonyLib;
using Steamworks;

namespace SteamworksEnforcer;

public static class Patches {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamAPI), nameof(SteamAPI.RestartAppIfNecessary))]
    public static void RestartAppIfNecessaryPrefix(ref AppId_t unOwnAppID) {
        unOwnAppID = new AppId_t(823130);
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
    public static void AwakePrefix() {
        string path = Path.Combine(Paths.GameRootPath, "steam_appid.txt");
        if (!File.Exists(path)) { File.WriteAllText(path, "823130"); }
    }
}