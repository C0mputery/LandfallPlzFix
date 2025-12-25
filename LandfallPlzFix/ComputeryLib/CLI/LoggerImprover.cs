using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using FMODUnity;
using HarmonyLib;
using Landfall.TABG;
using UnityEngine;

namespace ComputeryLib.CLI;

public static class LoggerImprover {
    public static void ImproveLoggersCheck() {
        if (!Plugin.Config.Bind("Log", "ImproveLoggers", true, "Removes a lot of the junk logs you prob don't care about").Value) { return; }

        try {
            Type nestedType = typeof(CommunityBackendAPI).GetNestedType("<>c__DisplayClass7_0", BindingFlags.NonPublic);
            MethodInfo? targetMethod = nestedType.GetMethod("<GameServerHeartbeat>b__0", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            Plugin.Harmony.Patch(targetMethod, new HarmonyMethod(typeof(LoggerImprover).GetMethod(nameof(SuppressCommunityBackendHeartbeatLogs))));
        }
        catch (Exception e) {
            Plugin.Logger.LogError(e);
            throw;
        }
        
    
    }
    
    public static IEnumerable<CodeInstruction> SuppressCommunityBackendHeartbeatLogs(IEnumerable<CodeInstruction> instructions) {
        CodeMatcher matcher = new CodeMatcher(instructions);
        matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Heartbeat sent! Code: "));
        matcher.RemoveInstructions(13);
        return matcher.InstructionEnumeration();
    }
}