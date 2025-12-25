using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputeryLib.CLI;
using ComputeryLib.Commands;
using ComputeryLib.Config;
using ComputeryLib.Utilities;
using ComputeryLib.Utilities.WorldUtility;
using HarmonyLib;
using UnityEngine;

namespace ComputeryLib;

[DefaultExecutionOrder(-int.MaxValue / 10)]
[BepInIncompatibility("citrusbird.tabg.citruslib")]
[BepInIncompatibility("com.starterpack.tabg.contagious")]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger = null!;
    public new static ConfigFile Config = null!;
    public static Harmony Harmony = new Harmony(PluginInfo.PLUGIN_GUID);
    
    private void Awake() {
        bool usingCli = ArgumentUtility.TryGetArgument("-pipeName", out string? pipeName);
        if (usingCli) { Harmony.PatchAll(typeof(ManualLogSourcePatch)); }
        
        Logger = base.Logger;
        Config = base.Config;
        
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        if (usingCli) {
            Logger.LogInfo("Detected CLI, initializing pipe handler.");
            PipeHandler.CreatePipe(pipeName!);
        }
        
        ApplyPatches();
        ApplyConditionalPatches();
        
        Logger.LogInfo($"Applied patches.");
    }
    
    private static void ApplyPatches() {
        Harmony.PatchAll(typeof(GameSettingsPatch));
        Harmony.PatchAll(typeof(ChatMessageCommandPatch));
        Harmony.PatchAll(typeof(ServerClientPatch));
    }
    
    private static void ApplyConditionalPatches() {
        if (Config.Bind("Log", "ImproveLoggers", true, "Removes a lot of the junk logs you prob don't care about").Value) { LoggerImprover.ApplyLoggerPatches(); }
    }

    private void Start() { ChatCommandManager.RegisterCommands(); }
}