using System.IO.Pipes;

namespace TwoWayAnonymousPipe;

public readonly struct TwoWayAnonymousPipeHandles(string serverToClientHandle, string clientToServerHandle) {
    public readonly string ServerToClientPipeHandle = serverToClientHandle;
    public readonly string ClientToServerPipeHandle = clientToServerHandle;

    public override string ToString() { return $"{ServerToClientPipeHandle}|{ClientToServerPipeHandle}"; }
    public static TwoWayAnonymousPipeHandles FromString(string data) { 
        string[] parts = data.Split('|');
        return new TwoWayAnonymousPipeHandles(parts[0], parts[1]);
    }
}

public abstract class TwoWayAnonymousPipeBase : IDisposable {
    protected PipeStream? InPipe;
    protected PipeStream? OutPipe;
    protected StreamWriter? Writer;
    protected StreamReader? Reader;

    public void SendMessage(string message) { Writer?.WriteLine(message); }
    public bool HasData() { return Reader?.Peek() != -1; }
    public string? ReceiveMessage() { return Reader?.ReadLine(); }

    public virtual void Dispose() {
        InPipe?.Dispose();
        OutPipe?.Dispose();
        Writer?.Dispose();
        Reader?.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class TwoWayAnonymousPipeServer : TwoWayAnonymousPipeBase {
    public TwoWayAnonymousPipeHandles InitializePipes() {
        InPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        OutPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        Writer = new StreamWriter(OutPipe) { AutoFlush = true };
        Reader = new StreamReader(InPipe);
        return new TwoWayAnonymousPipeHandles(((AnonymousPipeServerStream)OutPipe).GetClientHandleAsString(), ((AnonymousPipeServerStream)InPipe).GetClientHandleAsString());
    }
    
    public void CloseClientHandles() {
        ((AnonymousPipeServerStream?)InPipe)?.DisposeLocalCopyOfClientHandle();
        ((AnonymousPipeServerStream?)OutPipe)?.DisposeLocalCopyOfClientHandle();
    }
}

public class TwoWayAnonymousPipeClient : TwoWayAnonymousPipeBase {
    public void InitializePipes(TwoWayAnonymousPipeHandles handles) {
        InPipe = new AnonymousPipeClientStream(PipeDirection.In, handles.ServerToClientPipeHandle);
        OutPipe = new AnonymousPipeClientStream(PipeDirection.Out, handles.ClientToServerPipeHandle);
        Writer = new StreamWriter(OutPipe) { AutoFlush = true };
        Reader = new StreamReader(InPipe);
    }
}
