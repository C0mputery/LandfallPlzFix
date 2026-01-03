using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BinsCinematicMod;

public static class Patches {
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WilhelmChunker), nameof(WilhelmChunker.ChunkUpdate))]
    private static bool ChunkUpdatePrefix(ref WilhelmChunker __instance) {
        List<WilhelmChunkPiece> chunks = [];
        for (int x = 0; x < __instance.numberOfChunks; x++) {
            for (int y = 0; y < __instance.numberOfChunks; y++) {
                WilhelmChunkPiece chunk = __instance.chunks[x, y];
                if (__instance.loadedChunks.Contains(chunk)) { continue; }
                chunk.EnterChunk();
                chunks.Add(chunk);
            }
        }
        return false;
    }
    
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(CharacterGearHandler), nameof(CharacterGearHandler.AttachGear), typeof(Gear), typeof(Gear.GearType), typeof(bool))]
    public static IEnumerable<CodeInstruction> RemoveSetLayers(IEnumerable<CodeInstruction> instructions) {
        CodeMatcher matcher = new CodeMatcher(instructions);

        matcher.MatchForward(false,
            new CodeMatch(OpCodes.Ldsfld, AccessTools.Field(typeof(Player), nameof(Player.localPlayer))),
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(CharacterGearHandler), "m_player")),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Object), "op_Equality")),
            new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S)
        );

        if (matcher.IsInvalid)
        {
            Debug.LogError("BinsCinematicMod: Could not find code to patch in CharacterGearHandler.AttachGear");
            return matcher.InstructionEnumeration();
        }

        var branchInstruction = matcher.InstructionAt(4);
        var jumpLabel = (Label)branchInstruction.operand;

        var startPos = matcher.Pos;

        matcher.MatchForward(false, new CodeMatch(instruction => instruction.labels.Contains(jumpLabel)));

        if (matcher.IsInvalid)
        {
            Debug.LogError("BinsCinematicMod: Could not find jump target in CharacterGearHandler.AttachGear");
            return matcher.InstructionEnumeration();
        }

        var endPos = matcher.Pos;

        matcher.Start();
        matcher.Advance(startPos);
        matcher.RemoveInstructions(endPos - startPos);
        
        return matcher.InstructionEnumeration();
    }
}