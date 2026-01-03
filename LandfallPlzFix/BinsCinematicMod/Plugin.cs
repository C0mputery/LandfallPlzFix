using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BinsCinematicMod;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin { 
    public new static ManualLogSource Logger;
        
    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        
        Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        
        if (Config.Bind("General", "LOD Patch", true, "Disabled the LOD system.").Value) {
            harmony.PatchAll(typeof(WilhelmChunkerPatches));
            Logger.LogInfo("Applied LOD patch.");
        }
        
        if (Config.Bind("General", "Gear Layer Patch", true, "Makes gear always render").Value) {
            harmony.PatchAll(typeof(CharacterGearHandlerPatches));
            Logger.LogInfo("Applied Gear Layer patch.");
        }
        
        Logger.LogInfo($"Applied patches.");
    }
}