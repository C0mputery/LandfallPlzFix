using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BinsCinematicMod;

[HarmonyPatch(typeof(WilhelmChunker))]
public static class WilhelmChunkerPatches {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(WilhelmChunker.ChunkUpdate))]
    private static bool ChunkUpdatePrefix(ref WilhelmChunker __instance) {
        for (int x = 0; x < __instance.numberOfChunks; x++) {
            for (int y = 0; y < __instance.numberOfChunks; y++) {
                WilhelmChunkPiece chunk = __instance.chunks[x, y];
                if (__instance.loadedChunks.Contains(chunk)) { continue; }
                chunk.EnterChunk();
                __instance.loadedChunks.Add(chunk);
            }
        }
        return false;
    }
}   

[HarmonyPatch(typeof(CharacterGearHandler))]
public static class CharacterGearHandlerPatches {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(CharacterGearHandler.AttachGear), typeof(Gear), typeof(Gear.GearType), typeof(bool))]
    public static void AttachGearPostfix(ref CharacterGearHandler __instance) {
        foreach (Gear gear in __instance.m_gearObjects.Values) { gear.gameObject.SetLayerRecursively(13, 22); }
    }
}