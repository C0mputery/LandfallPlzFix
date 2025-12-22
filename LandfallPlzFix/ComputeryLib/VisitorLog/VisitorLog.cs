using System;
using Landfall.Network;

namespace ComputeryLib.VisitorLog;

public struct VistorInfo {
    public string PlayerName;
    public string SteamID;
    public string PlayFabID;
    public string EpicUserName;
    public DateTime FirstSeen;
    public DateTime LastSeen;
}

public static class VisitorLog {
    public static void LogVisitor(TABGPlayerServer player) {
    }
}