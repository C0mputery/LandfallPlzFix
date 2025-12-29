using ComputeryLib.CLI;
using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using HarmonyLib;
using Landfall.Network;
using Newtonsoft.Json;

namespace ComputeryLib.VisitorLog;

[HarmonyPatch(typeof(GameRoom))]
public class RemovePlayerGameRoomPatch {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GameRoom.RemovePlayer))]
    public static void RemovePlayerPostfix(ref TABGPlayerServer p) {
        PipeHandler? pipeHandler = PipeHandler.Instance;
        if (pipeHandler != null) {
            string json = JsonConvert.SerializeObject(new { type = "PlayerLeft", epicUserName = p.EpicUserName.ToString()});
            pipeHandler.SendMessage(json);
        }
    }
}