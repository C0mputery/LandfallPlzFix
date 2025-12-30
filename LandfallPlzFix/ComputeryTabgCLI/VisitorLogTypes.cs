namespace ComputeryTabgCLI;

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
    public List<DatedString> UnityIds { get; set; } = [];
    public List<DatedString> IpAddresses { get; set; } = [];
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public uint PermissionLevel { get; set; } = 0;
}