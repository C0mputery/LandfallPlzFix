using Epic.OnlineServices;
using Epic.OnlineServices.Auth;
using HarmonyLib;
using Landfall.Network;

namespace ComputeryLib.VisitorLog;

[HarmonyPatch(typeof(RoomInitRequestCommand))]
public class RoomInitRequestCommandPatch {
    [HarmonyPostfix]
    [HarmonyPatch(nameof(RoomInitRequestCommand.OnVerifiesEpicToken))]
    public static void OnVerifiesEpicTokenPostfix(ref VerifyIdTokenCallbackInfo data) {
        if (data.ResultCode != Result.Success || data.ClientData is not TABGPlayerServer tabgPlayerServer) { return; }
        VisitorLog.LogVisitor(tabgPlayerServer);
    }
}