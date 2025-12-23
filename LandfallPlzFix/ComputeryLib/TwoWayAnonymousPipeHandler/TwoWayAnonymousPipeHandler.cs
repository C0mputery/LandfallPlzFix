using ComputeryLib.Commands;
using TwoWayAnonymousPipe;
using UnityEngine;

namespace ComputeryLib.TwoWayAnonymousPipeHandler;

public class TwoWayAnonymousPipeHandler : MonoBehaviour {
    private bool _initialized = false;
    private readonly TwoWayAnonymousPipeClient _pipeClient = new TwoWayAnonymousPipeClient();

    public void InitializePipes(TwoWayAnonymousPipeHandles handles) {
        _pipeClient.InitializePipes(handles);
        _pipeClient.SendMessage(""); // Message to indicate that we should close the handlers
        _initialized = true;
    }
    public void Update() {
        if (!_initialized) { return; }
        if (_pipeClient.HasData()) {
            string? message = _pipeClient.ReceiveMessage();
            ChatCommandManager.HandleConsoleMessage(message ?? "");
        }
    }
    
    private void OnDestroy() { _pipeClient.Dispose(); }
}
