using System;
using System.Collections.Generic;
using Landfall.Network;

namespace ComputeryLib.VisitorLog;

public struct DatedString {
    public required string Value { get; set; }
    public required DateTime FirstSeen { get; set; } 
    public DateTime LastSeen { get; set; }
    
    public static void UpdateDatedStringSet(List<DatedString> set, string newValue, DateTime currentTime) {
        DatedString lastEntry = set[^1];
        if (lastEntry.Value == newValue) {
            lastEntry.LastSeen = currentTime;
            set[^1] = lastEntry;
        } else {
            set.Add(new DatedString {
                Value = newValue,
                FirstSeen = currentTime,
                LastSeen = currentTime
            });
        }
    }
}

public struct VisitorInfo {
    public required List<DatedString> DisplayNames { get; set; }
    public required List<DatedString> SteamIds { get; set; }
    public required List<DatedString> PlayfabIds { get; set; }
    public required DateTime FirstSeen { get; set; }
    public required DateTime LastSeen { get; set; }
}

public static class VisitorLog {
    private static readonly Dictionary<string, VisitorInfo> Visitors = new();
    
    public static void LogVisitor(TABGPlayerServer player) {
        string epicId = player.EpicUserName;
        
        DateTime currentTime = DateTime.UtcNow;
        
        if (!Visitors.TryGetValue(epicId, out VisitorInfo info)) {
            info = new VisitorInfo {
                DisplayNames = [ new DatedString {
                    Value = player.PlayerName,
                    FirstSeen = currentTime,
                    LastSeen = currentTime
                } ],
                SteamIds = [ new DatedString {
                    Value = player.SteamID,
                    FirstSeen = currentTime,
                    LastSeen = currentTime
                } ],
                PlayfabIds = [ new DatedString {
                    Value = player.PlayFabID,
                    FirstSeen = currentTime,
                    LastSeen = currentTime
                } ],
                FirstSeen = currentTime,
                LastSeen = currentTime
            };
            Visitors[epicId] = info;
        }
        else {
            info.LastSeen = currentTime;
            DatedString.UpdateDatedStringSet(info.DisplayNames, player.PlayerName, currentTime);
            DatedString.UpdateDatedStringSet(info.SteamIds, player.SteamID, currentTime);
            DatedString.UpdateDatedStringSet(info.PlayfabIds, player.PlayFabID, currentTime);
            Visitors[epicId] = info;
        }
    }
}