using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputeryLib.Commands;
using ComputeryLib.Config;
using ComputeryLib.Utilities.WorldUtility;
using HarmonyLib;

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
        
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        ApplyPatches();
        
        Logger.LogInfo($"Applied patches.");
    }
    
    private static void ApplyPatches() {
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        harmony.PatchAll(typeof(GameSettingsPatch));
        harmony.PatchAll(typeof(ChatMessageCommandPatch));
        harmony.PatchAll(typeof(ServerClientPatch));

    }

    private void Start() { ChatCommandManager.RegisterCommands(); }
}