using System;
using Epic.OnlineServices;
using Epic.OnlineServices.Connect;
using HarmonyLib;

namespace LandfallPlzFixClient.ProgressBarPatch;

/// <summary>
/// This fixes the issue when a progress bar is canceled early it creates a copy of the med item being used.
/// Again this is a mod, and is far from the proper way I would recommend fixing this in the actual game code.
/// </summary>
[HarmonyPatch(typeof(ProgressBar))]
public static class ProgressBarPatch {
    /// <summary>
    /// This prefix patch checks if the progress bar is healing when stopped, if it is it only runs StopAllProgress once rather than twice.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ProgressBar.StopAllProgress))]
    public static bool AwakePrefix(ProgressBar __instance) {
        if (!__instance.healing.isHealing) { return true; } // Let the original method run if not healing
        
        // The Cancel healing method already calls the StopAllProgress method on the progress bar so we don't want to run this method twice.
        __instance.healing.isHealing = false;
        __instance.healing.CancelHealing();
        return false;
    }
}