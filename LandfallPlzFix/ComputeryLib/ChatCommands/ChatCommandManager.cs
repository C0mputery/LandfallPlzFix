using Landfall.Network;

namespace ComputeryLib.ChatCommands;

public static class ChatCommandManager {
    public static bool HandleChatMessage(string message, byte sender, ServerClient world) {
        if (message.Length == 0 || message[0] != '/') { return false; }
        
        
        
        return true;
    }
}