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

[BepInIncompatibility("citrusbird.tabg.citruslib")]
[BepInIncompatibility("com.starterpack.tabg.contagious")]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger = null!;
    public new static ConfigFile Config = null!;
        
    private void Awake() {
        Logger = base.Logger;
        Config = base.Config;

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        TryCreatePipe();
        ApplyPatches();
        Logger.LogInfo($"Applied patches.");
    }

    private static void TryCreatePipe() {
        if (!ArgumentUtility.TryGetArgument("-pipeName", out string? pipeName)) {
            Logger.LogInfo("No pipe handles argument found, likely not running the CLI.");
            return;
        }
        // Prevent LandLog from using ugly console output
        // TODO: tranpiler patch to resolve the time being on the wrong line
        LandLog.checkedHeadless = true;
        LandLog.Headless = false;
        
        GameObject pipeHandlerObject = new GameObject("TwoWayAnonymousPipeHandler");
        DontDestroyOnLoad(pipeHandlerObject);
        pipeHandlerObject.AddComponent<PipeHandler>().InitializePipe(pipeName);
    }
    
    private static void ApplyPatches() {
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(GameSettingsPatch));
        harmony.PatchAll(typeof(ChatMessageCommandPatch));
        harmony.PatchAll(typeof(ServerClientPatch));
    }

    private void Start() { ChatCommandManager.RegisterCommands(); }
}