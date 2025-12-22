using ComputeryLib.Utilities;
using Landfall.Network;

namespace ComputeryLib.ChatCommands;

public static class BasicChatCommands {
    [ChatCommand("start", "Starts the game with default countdown or specified countdown in seconds.", 1)]
    public static void StartCommand(string[] arguments, TABGPlayerServer sender, ServerClient world) {
        if (arguments.Length < 1) {
            world.GameRoomReference.StartCountDown(world.GameRoomReference.CurrentGameSettings.Countdown);
            return;
        }
        
        if (!int.TryParse(arguments[0], out int timeInSeconds) || timeInSeconds <= 0) {
            PlayerInteractionUtilities.SendPrivateMessage("Invalid time specified. Must be a positive integer.", sender, world);
            return;
        }

        world.GameRoomReference.StartCountDown(timeInSeconds);
    }
}