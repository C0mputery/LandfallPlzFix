using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ComputeryLib.ImprovedConfig;
using HarmonyLib;

namespace ComputeryLib;

[BepInIncompatibility("citrusbird.tabg.citruslib")]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin {
    public new static ManualLogSource Logger;
    public new static ConfigFile Config;
        
    private void Awake() {
        Logger = base.Logger;
        Config = base.Config;

        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginInfo.PLUGIN_GUID);
        Logger.LogInfo($"Applied patches.");
    }
}