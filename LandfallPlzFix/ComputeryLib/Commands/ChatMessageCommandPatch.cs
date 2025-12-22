using System.Text;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.Commands;

[HarmonyPatch(typeof(ChatMessageCommand))]
public static class ChatMessageCommandPatch {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ChatMessageCommand.Run))]
    public static bool RunPrefix(byte[] msgData, ServerClient world, byte sender) {
        byte length = msgData[1];
        string message = Encoding.Unicode.GetString(msgData, 2, length);
        
        TABGPlayerServer senderPlayer = world.GameRoomReference.FindPlayer(sender);
        if (senderPlayer == null) { return true; }
        return !ChatCommandManager.HandleChatMessage(message, senderPlayer, world);
    }
}