using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputeryLib.Commands;
using ComputeryLib.Config;
using ComputeryLib.Utilities;
using ComputeryLib.Utilities.WorldUtility;
using HarmonyLib;
using TwoWayAnonymousPipe;
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
        if (!ArgumentUtility.TryGetArgument("-pipeHandles", out string? pipeHandlesString)) {
            Logger.LogInfo("No pipe handles argument found, likely not running the CLI.");
            return;
        }
        TwoWayAnonymousPipeHandles pipeHandles = TwoWayAnonymousPipeHandles.FromString(pipeHandlesString);
        
        // Prevent LandLog from using ugly console output
        LandLog.checkedHeadless = true;
        LandLog.Headless = false;
        
        GameObject pipeHandlerObject = new GameObject("TwoWayAnonymousPipeHandler");
        DontDestroyOnLoad(pipeHandlerObject);
        pipeHandlerObject.AddComponent<TwoWayAnonymousPipeHandler.TwoWayAnonymousPipeHandler>().InitializePipes(pipeHandles);
    }
    
    private static void ApplyPatches() {
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(GameSettingsPatch));
        harmony.PatchAll(typeof(ChatMessageCommandPatch));
        harmony.PatchAll(typeof(ServerClientPatch));
    }

    private void Start() { ChatCommandManager.RegisterCommands(); }
}