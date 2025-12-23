using UnityEngine;

namespace ComputeryLib.CLI;

public class PipeHandler : MonoBehaviour {
    private bool _initialized = false;

    public void InitializePipe(string name) {
        _initialized = true;
    }
    public void Update() {
        if (!_initialized) { return; }
    }
    
    private void OnDestroy() {  }
}
