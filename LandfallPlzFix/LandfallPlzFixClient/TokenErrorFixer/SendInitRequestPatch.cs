using System;
using Epic.OnlineServices;
using HarmonyLib;
using Landfall.Network;
using Epic.OnlineServices.Connect;

namespace LandfallPlzFixClient.TokenErrorFixer;
/// <summary>
/// This is a hacky way to resolve the Token Authentication Error.
/// A proper fix would involve refactoring the EOSSDKComponent to handle token expiration and renewal.
/// I am not doing that here just because this is a BepInEx mod so it would be annoying with Harmony.
///
/// Epics Docs on the topic:
/// https://dev.epicgames.com/docs/epic-games-store/testing-guide#connect-interface-login-and-access-token-renewal
/// </summary>
[HarmonyPatch(typeof(ServerConnector), nameof(ServerConnector.SendInitRequest))]
public static class SendInitRequestPatch {
    /// <summary>
    /// This prefix patch checks if the user token has expired before sending the init request.
    /// If it's expired it tries to log in again, and if that is successful, it runs the method again.
    /// </summary>
    /// <param name="__instance"> The instance of ServerConnector, Harmony provides this. </param>
    /// <returns> True and False dictate whether to run the original method or not. Handled by Harmony </returns>
    [HarmonyPrefix]
    public static bool SendInitRequestPatchPrefix(ref ServerConnector __instance) {
        Result userToken = EOSSDKComponent.GetUserToken(out _); // Check the user token status, the SendInitRequest method does this again, but this is again just a Harmony 
        
        Plugin.Logger.LogInfo($"User token check returned {Enum.GetName(typeof(Result), userToken)}");
        if (userToken != Result.InvalidAuth) {
            Plugin.Logger.LogInfo($"User token valid, sending init request as normal.");
            
            // If our token is not expired, continue as normal.
            return true;
        }
        
        Plugin.Logger.LogInfo($"User token expired, attempting to re-login before sending init request.");
        
        // The login stuff is done the same way the EOSSDKComponent does it.
        LoginOptions loginOptions = new LoginOptions {
            Credentials = new Credentials {
                Token = EOSSDKComponent.ByteArrayToString(PlayFabManager.SteamTicket.Item2),
                Type = ExternalCredentialType.SteamSessionTicket
            }
        };
        
        ServerConnector instance = __instance; // This captures the instance for use in the callback........... if you where wondering, it's not pointless.
        EOSSDKComponent.s_PlatformInterface.GetConnectInterface().Login(ref loginOptions, new object(), delegate(ref LoginCallbackInfo loginCallbackInfo) {
            if (loginCallbackInfo.ResultCode == Result.Success) {
                Plugin.Logger.LogInfo($"Re-login successful, resending init request.");
                
                // If we successfully logged in again, try sending the init request again.
                // In most cases it should not be problematic that this is recursive, and again, this is just a mod.
                instance.SendInitRequest();
            } else {
                Plugin.Logger.LogError($"Re-login failed with error: {loginCallbackInfo.ResultCode}. Failing to main menu.");
                
                // If we failed, fail in the same way as SendInitRequest.
                instance.LastTokenError = userToken.ToString();
                ServerConnector.MenuError = MenuError.TokenError;
                instance.OnMainMenu();
            }
        });
        
        return false;
    }
}