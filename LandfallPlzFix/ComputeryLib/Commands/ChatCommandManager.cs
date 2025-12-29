using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using ComputeryLib.Utilities;
using Landfall.Network;

namespace ComputeryLib.Commands;

public struct ChatCommandContext {
    public int PermissionLevel;
    public string Description;
    public ChatCommandHandler Command;
}

public delegate void ChatCommandHandler(string[] arguments, TABGPlayerServer? sender, ServerClient world);

public static class ChatCommandManager {
    private static readonly ConfigFile ChatCommandsConfig = new ConfigFile(Path.Combine(Paths.ConfigPath, "ChatCommands.cfg"), true);
    
    private static readonly Dictionary<string, ChatCommandContext> Commands = new();
    public static bool HandleChatMessage(string message, TABGPlayerServer sender, ServerClient world) {
        if (message.Length == 0 || message[0] != '/') { return false; }
        string[] parts = message.ToLower().Substring(1).Split(' ');
        string commandName = parts[0];
        string[] arguments = parts.Skip(1).ToArray();
        
        if (!Commands.TryGetValue(commandName, out ChatCommandContext chatCommandContext)) {
            PlayerInteractionUtilities.PrivateMessage($"Unknown command: {commandName}, type /help for a list of commands you can use.", sender, world);
            return true;
        }
        
        uint userPermissionLevel = GetUserPermissionLevel(sender);
        if (userPermissionLevel < chatCommandContext.PermissionLevel) {
            PlayerInteractionUtilities.PrivateMessage($"You do not have permission to use the /{commandName} command.", sender, world);
            return true;
        }
        
        try { chatCommandContext.Command(arguments, sender, world); }
        catch (Exception e) {
            Plugin.Logger.LogError($"Error executing command '{commandName}': {e.Message}");
            PlayerInteractionUtilities.PrivateMessage($"An error occurred while executing the command: {e.Message}", sender, world);
        }
        
        return true;
    }

    public static void HandleConsoleMessage(string message) {
        string[] parts = message.ToLower().Split(' ');
        string commandName = parts[0].ToLower();
        string[] arguments = parts.Skip(1).ToArray();

        if (!Commands.TryGetValue(commandName, out ChatCommandContext chatCommandContext)) {
            Plugin.Logger.LogInfo($"Unknown command: {commandName}, type help for a list of commands you can use."); 
            return;
        }
        chatCommandContext.Command(arguments, null, WorldUtilities.GetWorld());
    }

    public static void RegisterCommands() {
        Commands.Clear();
        
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            foreach (Type type in assembly.GetTypes()) {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) {
                    IEnumerable<ChatCommandAttribute>? attributes = method.GetCustomAttributes<ChatCommandAttribute>();
                    foreach (ChatCommandAttribute attribute in attributes) {
                        if (attribute == null) { continue; }

                        string commandName = attribute.CommandName.ToLower();

                        if (Commands.ContainsKey(commandName)) {
                            Plugin.Logger.LogWarning($"Command '{commandName}' has already been registered. Skipping duplicate registration from method '{method.Name}' in type '{type.FullName}'.");
                            continue;
                        }

                        Commands[commandName] = new ChatCommandContext {
                            Command = (ChatCommandHandler)Delegate.CreateDelegate(typeof(ChatCommandHandler), method),
                            Description = attribute.Description,
                            PermissionLevel = ChatCommandsConfig.Bind("Permissions", commandName, attribute.DefaultPermissionLevel, $"Permission level for the /{commandName} command.\n{attribute.Description}").Value
                        };
                    }
                }
            }
        }
    }
    
    private static uint GetUserPermissionLevel(TABGPlayerServer player) { 
        return VisitorLog.VisitorLog.GetPermissionLevel(player.EpicUserName);
    }
    
    [ChatCommand("help", "Displays the description of a command or lists all commands you can use.", 0)]
    public static void HelpCommand(string[] arguments, TABGPlayerServer? sender, ServerClient world) {
        if (arguments.Length == 0) {
            StringBuilder commandListBuilder = new();
            uint userPermissionLevel = sender != null ? GetUserPermissionLevel(sender) : uint.MaxValue;
            foreach (KeyValuePair<string, ChatCommandContext> command in Commands) {
                if (userPermissionLevel >= command.Value.PermissionLevel) { commandListBuilder.Append($"{command.Key}, "); }
            }
            string commandList = commandListBuilder.ToString().TrimEnd(',', ' ');
            PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"Available commands: {commandList}", sender, world);
        } 
        else {
            string commandName = arguments[0].ToLower();
            if (Commands.TryGetValue(commandName, out ChatCommandContext chatCommandContext)) { PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"/{commandName}: {chatCommandContext.Description}", sender, world); } 
            else { PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"No description found for command: {commandName}", sender, world); }
        }
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class ChatCommandAttribute(string commandName, string description, int defaultPermissionLevel) : Attribute {
    public readonly string CommandName = commandName;
    public readonly string Description = description;
    public readonly int DefaultPermissionLevel = defaultPermissionLevel;
}
