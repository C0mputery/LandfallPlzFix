using System;
using System.IO;
using System.Text;
using BepInEx.Logging;
using ComputeryLib.Utilities;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.MessageLogging;

public static class MessageLogger {
    private static readonly string MessageLogDirectory = Path.Combine(PersistantDataUtility.PersistentDataPath, "MessageLogs");
    private static readonly ManualLogSource Logger = new ManualLogSource("Player Messages");

    static MessageLogger() {
        if (!Directory.Exists(MessageLogDirectory)) { Directory.CreateDirectory(MessageLogDirectory); }
    }
    
    private static void LogMessage(string displayName, string epicID, string message, bool wasThrown) {
        string logFilePath = Path.Combine(MessageLogDirectory, $"{epicID}.log");
        string logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {(wasThrown ? "[THROWN] " : "")}{message}";
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
        Logger.LogInfo($"[{displayName}] {logEntry}");
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ThrowChatMessageCommand), nameof(ThrowChatMessageCommand.Run))]
    private static void ThrowChatMessageCommandPostfix(byte[] data, ServerClient world, byte sender) {
        TABGPlayerServer player = world.GameRoomReference.FindPlayer(sender);
        if (player == null) { return; }
        
        using MemoryStream memoryStream = new MemoryStream(data, 1, data.Length - 1);
        using BinaryReader binaryReader = new BinaryReader(memoryStream);
        byte length = binaryReader.ReadByte();
        string message = Encoding.Unicode.GetString(binaryReader.ReadBytes(length));
        
        LogMessage(player.PlayerName, player.EpicUserName, message, true);
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ChatMessageCommand), nameof(ChatMessageCommand.Run))]
    private static void ChatMessageCommandPrefix(byte[] msgData, ServerClient world, byte sender) {
        TABGPlayerServer player = world.GameRoomReference.FindPlayer(sender);
        if (player == null) { return; }
        
        using MemoryStream memoryStream = new MemoryStream(msgData, 1, msgData.Length - 1);
        using BinaryReader binaryReader = new BinaryReader(memoryStream);
        byte length = binaryReader.ReadByte();
        string message = Encoding.Unicode.GetString(binaryReader.ReadBytes(length));
        
        LogMessage(player.PlayerName, player.EpicUserName, message, false);
    }
}