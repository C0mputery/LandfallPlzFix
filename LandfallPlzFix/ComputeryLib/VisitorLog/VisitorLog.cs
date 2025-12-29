using System;
using System.Collections.Generic;
using System.IO;
using ComputeryLib.Utilities;
using ENet;
using Newtonsoft.Json;
using Landfall.Network;
using PlayFab;
using PlayFab.ClientModels;

namespace ComputeryLib.VisitorLog;

public struct DatedString {
    public string Value { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    
    public static void UpdateDatedStringSet(List<DatedString> set, string value, DateTime currentTime) {
        if (set.Count > 0) {
            int lastIndex = set.Count - 1;
            DatedString lastEntry = set[lastIndex];
            if (lastEntry.Value == value) {
                lastEntry.LastSeen = currentTime;
                set[lastIndex] = lastEntry;
                return;
            }
        }
        set.Add(new DatedString {
            Value = value,
            FirstSeen = currentTime,
            LastSeen = currentTime
        });
    }
}

public struct VisitorInfo() {
    public List<DatedString> DisplayNames { get; set; } = [];
    public List<DatedString> SteamIds { get; set; } = [];
    public List<DatedString> PlayfabIds { get; set; } = [];
    public List<DatedString> IpAddresses { get; set; } = [];
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public uint PermissionLevel { get; set; } = 0;
}

public static class VisitorLog {
    private static readonly string VisitorLogPath = Path.Combine(PersistantDataUtility.PersistentDataPath, "VisitorLog.json");
    private static readonly Dictionary<string, VisitorInfo> Visitors;
    
    static VisitorLog() {
        if (File.Exists(VisitorLogPath)) {
            string json = File.ReadAllText(VisitorLogPath);
            try { Visitors = JsonConvert.DeserializeObject<Dictionary<string, VisitorInfo>>(json) ?? new Dictionary<string, VisitorInfo>(); }
            catch (JsonException) { Visitors = new Dictionary<string, VisitorInfo>(); }
        }
        else { Visitors = new Dictionary<string, VisitorInfo>(); }
    }

    private static void SaveVisitorLog() { File.WriteAllText(VisitorLogPath, JsonConvert.SerializeObject(Visitors, Formatting.Indented)); }
    
    public static uint GetPermissionLevel(string epicUserName) {
        return Visitors.TryGetValue(epicUserName, out VisitorInfo visitorInfo) ? visitorInfo.PermissionLevel : 0;
    }

    public static void LogVisitor(TABGPlayerServer player) {
        DateTime currentTime = DateTime.UtcNow;
        
        if (!PlayFabClientAPI.IsClientLoggedIn()) {
            LoginWithCustomIDRequest loginRequest = new LoginWithCustomIDRequest { CustomId = Guid.NewGuid().ToString(), CreateAccount = true };
            PlayFabClientAPI.LoginWithCustomID(loginRequest, 
                loginResult => { TryToGetSteamFromLeaderboard(player, currentTime); },
                error => { LogVisitor(player, "Unknown...", currentTime);
            });
        }
        else { TryToGetSteamFromLeaderboard(player, currentTime); }
    }

    private static void TryToGetSteamFromLeaderboard(TABGPlayerServer player, DateTime currentTime) {
        GetLeaderboardAroundPlayerRequest getLeaderboardAroundPlayerRequest = new GetLeaderboardAroundPlayerRequest {
            StatisticName = "PlayerMatchLosses",
            MaxResultsCount = 1,
            PlayFabId = player.PlayFabID,
            ProfileConstraints = new PlayerProfileViewConstraints { ShowLinkedAccounts = true }
        };
        
        PlayFabClientAPI.GetLeaderboardAroundPlayer(getLeaderboardAroundPlayerRequest, result => {
            if (result.Leaderboard.Count > 0) {
                PlayerLeaderboardEntry entry = result.Leaderboard[0];
                foreach (LinkedPlatformAccountModel account in entry.Profile.LinkedAccounts) {
                    if (account.Platform != LoginIdentityProvider.Steam) { continue; }
                    string steamId = account.PlatformUserId;
                    LogVisitor(player, steamId, currentTime);
                    return;
                }
            }
            LogVisitor(player, "Unknown...", currentTime);
        }, error => {
            LogVisitor(player, "Unknown...", currentTime);
        });
    }

    private static void LogVisitor(TABGPlayerServer player, string steamId, DateTime currentTime) {
        if (!Visitors.TryGetValue(player.EpicUserName, out VisitorInfo visitorInfo)) {
            visitorInfo = new VisitorInfo { 
                FirstSeen = currentTime,
                LastSeen = currentTime
            };
        }

        DatedString.UpdateDatedStringSet(visitorInfo.DisplayNames, player.PlayerName, currentTime);
        DatedString.UpdateDatedStringSet(visitorInfo.SteamIds, steamId, currentTime);
        DatedString.UpdateDatedStringSet(visitorInfo.PlayfabIds, player.PlayFabID, currentTime);
        if (ServerClient.m_Server is EnetServer server) {
            if (server.m_IndexToENetPeerDic.TryGetValue(player.PlayerIndex, out Peer enetPeer)) {
                DatedString.UpdateDatedStringSet(visitorInfo.IpAddresses, enetPeer.IP, currentTime);
            }
        }
        visitorInfo.LastSeen = currentTime;
        
        Visitors[player.EpicUserName] = visitorInfo;
        SaveVisitorLog();
    }
}