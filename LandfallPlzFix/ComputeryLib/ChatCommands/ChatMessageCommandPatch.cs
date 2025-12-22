using System.Text;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.ChatCommands;

[HarmonyPatch(typeof(ChatMessageCommand))]
public static class ChatMessageCommandPatch {
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ChatMessageCommand.Run))]
    public static bool RunPrefix(byte[] msgData, ServerClient world, byte sender) {
        
        byte length = msgData[1];
        string message = Encoding.Unicode.GetString(msgData, 2, length);
        
        return !ChatCommandManager.HandleChatMessage(message, sender, world); // If handled we don't want to send the message.
    }
}