using HarmonyLib;

namespace ComputeryLib.LogImprovements;

[HarmonyPatch(typeof(LandLog))]
public class LandLogPatch {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(LandLog.Log))]
    public static bool LogPrefix(string logMessage, object context) {
        
    }
}