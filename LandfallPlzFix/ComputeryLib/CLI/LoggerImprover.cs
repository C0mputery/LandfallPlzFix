using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Landfall.TABG;
using UnityEngine;

namespace ComputeryLib.CLI;

public static class LoggerImprover {
    public static void ApplyLoggerPatches() {
        try {
            Type nestedType = typeof(CommunityBackendAPI).GetNestedType("<>c__DisplayClass7_0", BindingFlags.NonPublic);
            MethodInfo? targetMethod = nestedType.GetMethod("<GameServerHeartbeat>b__0", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            Plugin.Harmony.Patch(targetMethod, transpiler: new HarmonyMethod(typeof(LoggerImprover), nameof(SuppressCommunityBackendHeartbeatLogs)));
        }
        catch (Exception e) { Plugin.Logger.LogError(e); }
        
        Plugin.Harmony.PatchAll(typeof(LoggerImprover));
    } 
    
    public static IEnumerable<CodeInstruction> SuppressCommunityBackendHeartbeatLogs(IEnumerable<CodeInstruction> instructions) {
        CodeMatcher matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Heartbeat sent! Code: "));
        matcher.RemoveInstructions(13);
        return matcher.InstructionEnumeration();
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(LandLog), nameof(LandLog.Log), typeof(string), typeof(object))]
    public static IEnumerable<CodeInstruction> FixLandLog(IEnumerable<CodeInstruction> instructions) {
        CodeMatcher matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Console), nameof(Console.WriteLine), [typeof(string)])));
        matcher.Set(OpCodes.Call, AccessTools.Method(typeof(Console), nameof(Console.Write), [typeof(string)]));
        return matcher.InstructionEnumeration();
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(FMODUnity.RuntimeManager), nameof(FMODUnity.RuntimeManager.LoadBank), typeof(string), typeof(bool))]
    [HarmonyPatch(typeof(FMODUnity.RuntimeManager), nameof(FMODUnity.RuntimeManager.CreateInstance), typeof(string))]
    public static bool Disable() { return false; }
}