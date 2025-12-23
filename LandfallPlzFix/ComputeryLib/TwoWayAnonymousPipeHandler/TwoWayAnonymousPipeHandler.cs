using ComputeryLib.Commands;
using TwoWayAnonymousPipe;
using UnityEngine;

namespace ComputeryLib.TwoWayAnonymousPipeHandler;

public class TwoWayAnonymousPipeHandler : MonoBehaviour {
    private bool _initialized = false;
    private readonly TwoWayAnonymousPipeClient _pipeClient = new TwoWayAnonymousPipeClient();

    public void InitializePipes(TwoWayAnonymousPipeHandles handles) {
        _pipeClient.InitializePipes(handles);
        _initialized = true;
    }
    public void Update() {
        if (!_initialized) { return; }
        while (_pipeClient.TryReceiveMessage(out string? message)) {
            ChatCommandManager.HandleConsoleMessage(message ?? "");
            LandLog.Log($"Received pipe message: {message}");
        }
    }
    
    private void OnDestroy() { _pipeClient.Dispose(); }
}
