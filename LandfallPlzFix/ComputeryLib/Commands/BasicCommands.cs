using System;
using System.Collections.Generic;
using System.Linq;
using ComputeryLib.Utilities;
using Landfall.Network;

namespace ComputeryLib.Commands;

public static class BasicCommands {
    [ChatCommand("start", "Starts the game with default countdown or specified countdown in seconds.", 1)]
    public static void StartCommand(string[] arguments, TABGPlayerServer? sender, ServerClient world) {
        if (arguments.Length < 1) {
            world.GameRoomReference.StartCountDown();
            return;
        }
        
        if (!float.TryParse(arguments[0], out float timeInSeconds) || timeInSeconds < 0) {
            PlayerInteractionUtilities.PrivateMessageOrConsoleLog("Invalid time specified. Must be a non-negative number.", sender, world);
            return;
        }
        if (timeInSeconds == 0) {
            timeInSeconds = float.Epsilon;
        }

        world.GameRoomReference.StartCountDown(timeInSeconds);
    }

    [ChatCommand("setlevel", "Sets the permission level of a player. Usage: /setlevel <player_name|player_id|epic_username> <level>", 10)]
    public static void SetLevelCommand(string[] arguments, TABGPlayerServer? sender, ServerClient world) {
        if (arguments.Length < 2) {
            PlayerInteractionUtilities.PrivateMessageOrConsoleLog("Usage: /setlevel <player_name|player_id|epic_username> <level>", sender, world);
            return;
        }

        if (!uint.TryParse(arguments[^1], out uint level)) {
            PlayerInteractionUtilities.PrivateMessageOrConsoleLog("Invalid level specified. Must be a non-negative integer.", sender, world);
            return;
        }

        string searchValue = string.Join(" ", arguments.Take(arguments.Length - 1));
        
        List<TABGPlayerServer>? players = world.GameRoomReference.Players;
        foreach (TABGPlayerServer player in players) {
            if (player.PlayerName == searchValue) {
                VisitorLog.VisitorLog.SetPermissionLevel(player.EpicUserName, level);
                PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"Set permission level {level} for player {player.PlayerName}.", sender, world);
                return;
            }
            
            if (player.PlayerIndex.ToString() == searchValue) {
                VisitorLog.VisitorLog.SetPermissionLevel(player.EpicUserName, level);
                PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"Set permission level {level} for player {player.PlayerName} (ID: {player.PlayerIndex}).", sender, world);
                return;
            }
            
            if (player.EpicUserName == searchValue) {
                VisitorLog.VisitorLog.SetPermissionLevel(player.EpicUserName, level);
                PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"Set permission level {level} for player {player.PlayerName} ({player.EpicUserName}).", sender, world);
                return;
            }
        }
        
        PlayerInteractionUtilities.PrivateMessageOrConsoleLog($"Player not found: {searchValue}", sender, world);
    }
}