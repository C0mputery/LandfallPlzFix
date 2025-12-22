using ComputeryLib.Utilities;
using Landfall.Network;

namespace ComputeryLib.ChatCommands;

public static class ChatCommandManager {
    public static bool HandleChatMessage(string message, TABGPlayerServer sender, ServerClient world) {
        if (message.Length == 0 || message[0] != '/') { return false; }
         
        PlayerInteractionUtilities.SendPrivateMessage($"Command '{message}' received from player {sender}, but no command handlers are registered. Try again if needed", sender, world);
        
        return true;
    }
}