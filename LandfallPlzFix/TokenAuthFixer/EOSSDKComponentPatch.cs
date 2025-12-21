using System;
using Epic.OnlineServices;
using HarmonyLib;
using Epic.OnlineServices.Connect;

namespace TokenAuthFixer;

/// <summary>
/// This is a hacky way to resolve the Token Authentication Error.
/// A proper fix would involve refactoring the EOSSDKComponent to handle token expiration and renewal.
/// I am not doing that here just because this is a BepInEx mod so it would be annoying with Harmony.
///
/// Epics Docs on the topic:
/// https://dev.epicgames.com/docs/epic-games-store/testing-guide#connect-interface-login-and-access-token-renewal
/// </summary>
[HarmonyPatch(typeof(EOSSDKComponent), nameof(EOSSDKComponent.Awake))]
public static class EOSSDKComponentPatch {
    /// <summary>
    /// This prefix patch checks if the user token whenever the main menu loads.
    /// If it's expired it tries to log in again.
    /// </summary>
    [HarmonyPrefix]
    public static void AwakePrefix() {
        if (EOSSDKComponent.s_PlatformInterface == null) { return; } // EOS not initialized yet
        
        Result userToken = EOSSDKComponent.GetUserToken(out _); // Check the user token status
        
        Plugin.Logger.LogInfo($"User token check returned {Enum.GetName(typeof(Result), userToken)}");
        if (userToken != Result.InvalidAuth) { return; }
        
        // The login stuff is done the same way the EOSSDKComponent does it.
        LoginOptions loginOptions = new LoginOptions {
            Credentials = new Credentials {
                Token = EOSSDKComponent.ByteArrayToString(PlayFabManager.SteamTicket.Item2),
                Type = ExternalCredentialType.SteamSessionTicket
            }
        };
        
        EOSSDKComponent.s_PlatformInterface.GetConnectInterface().Login(ref loginOptions, new object(), delegate(ref LoginCallbackInfo loginCallbackInfo) {
            Plugin.Logger.LogInfo($"Re-login attempt returned {Enum.GetName(typeof(Result), loginCallbackInfo.ResultCode)}");
        });
    }
}