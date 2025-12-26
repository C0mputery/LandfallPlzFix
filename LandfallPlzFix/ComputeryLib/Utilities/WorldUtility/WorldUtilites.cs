using System;
using System.Diagnostics.CodeAnalysis;
using Landfall.Network;

namespace ComputeryLib.Utilities.WorldUtility;

public static class WorldUtilites {
    private static ServerClient? _worldClient;
    public static bool TryGetWorld(out ServerClient? world) {
        if (_worldClient != null) { world = _worldClient; return true; }
        world = null;
        return false;
    }
    
    public static ServerClient GetWorld() { return _worldClient == null ? throw new InvalidOperationException("World client has not been set yet.") : _worldClient; }
    
    internal static void SetWorldClient(ServerClient world) { _worldClient = world; }
}