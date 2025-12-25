using System;
using BepInEx.Logging;
using HarmonyLib;

namespace ComputeryLib.CLI;

/// <summary>
/// Lord forgive me.
/// </summary>
[HarmonyPatch(typeof(ManualLogSource))]
public class ManualLogSourcePatch {
    [HarmonyPrefix]
    [HarmonyPatch(MethodType.Constructor, typeof(string))]
    public static bool ConstructorPrefix(ManualLogSource __instance, ref string sourceName) {
        if (sourceName != "Console") { return true; }
        sourceName = "Yo dipshit don't name your logger `Console` pls and tank you";
        return false;
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ManualLogSource.Log))]
    public static bool LogPrefix(ManualLogSource __instance, LogLevel level, object data) {
        if (__instance.SourceName == "Console") { return false; } // lets just hope nobody makes a logger with the name "Console"
        Console.WriteLine(new LogEventArgs(data, level, __instance));
        return true;
    }
}