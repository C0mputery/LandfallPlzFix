using System.Diagnostics;
using System.IO.Pipes;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace ComputeryTabgCLI;

internal static class Program {
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static Process? _serverProcess;
    private static IApplication _app = null!;
    private static NamedPipeServerStream? _pipeServer;
    private static StreamWriter? _pipeWriter;
    private static Timer? _keepAliveTimer;
    
    private static TextView _logView = null!;
    private static TextField _commandInput = null!;

    private static void LogLine(string text) {
        if (!string.IsNullOrEmpty(_logView.Text)) { text += $"\n{text}"; }
        _app.Invoke(() => { LogTextApp(text); });
    }

    private static void LogText(string text) {
        _app.Invoke(() => { LogTextApp(text); });
    }

    private static void LogTextApp(string text) {
        int currentTopRow = _logView.TopRow;
        int currentLines = _logView.Lines;
        int visibleHeight = _logView.GetContentSize().Height;
        int maxScroll = Math.Max(0, currentLines - visibleHeight);
        bool wasAtBottom = (currentLines == 0) || (currentTopRow >= maxScroll - 1);
        
        _logView.Text += text;

        if (wasAtBottom) {
            _logView.MoveEnd();
            ClampLogScroll();
        }
        else { _logView.TopRow = currentTopRow; }
    }

    private static void ClampLogScroll() {
        int lines = _logView.Lines;
        int visibleHeight = _logView.GetContentSize().Height;
        int maxScroll = Math.Max(0, lines - visibleHeight);
        if (_logView.TopRow > maxScroll) { _logView.TopRow = maxScroll; }
        _logView.SetNeedsDraw();
    }

    public static void Main(string[] args) {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;
        
        _app = Application.Create().Init();
        Window top = new Window() { BorderStyle = LineStyle.None };
        
        _logView = new TextView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
            ReadOnly = true,
            WordWrap = true,
            BorderStyle = LineStyle.Rounded
        };
        _logView.Initialized += (_, _) => {
            // Only way I found to disable context items is in this dumb hacky way
            View deleteAll = _logView.ContextMenu!.Root!.SubViews.ElementAt(1);
            View cut = _logView.ContextMenu!.Root.SubViews.ElementAt(3);
            _logView.ContextMenu!.Root.Remove(deleteAll);
            _logView.ContextMenu!.Root.Remove(cut);

        };
        _logView.DrawingText += (_, _) => { ClampLogScroll(); };
        
        _commandInput = new TextField {
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            Height = Dim.Absolute(3),
            BorderStyle = LineStyle.Rounded
        };
        top.Add(_commandInput);
        
        top.Add(_logView);
        
        _ = RunServerAsync(CancellationTokenSource.Token);
        
        _app.Run(top);
        _keepAliveTimer?.Dispose();
        top.Dispose();
        _app.Dispose();
        CleanupServer();
    }
    
    private static void OnProcessExit(object? sender, EventArgs e) { CleanupServer(); }
    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e) {
        e.Cancel = true;
        CleanupServer();
        Environment.Exit(0);
    }

    private static void CleanupServer() {
        try { _keepAliveTimer?.Dispose(); }
        catch { /* Ignored */ }

        try { CancellationTokenSource.Cancel(); }
        catch { /* Ingorned */ }

        try {
            _pipeWriter?.Dispose();
            _pipeServer?.Dispose();
        }
        catch { /* Ignored */ }

        if (_serverProcess != null && !_serverProcess.HasExited) {
            try {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(5000);
                _serverProcess.Dispose();
            }
            catch (Exception ex) { Console.Error.WriteLine($"Error closing server: {ex.Message}"); }
            finally { _serverProcess = null; }
        }
        CancellationTokenSource.Dispose();
    }
    
    private static async Task RunServerAsync(CancellationToken cancellationToken) {
        string unityAppPath = @"C:\Users\Computery\Desktop\LandfallPlzFix\Server\TABG.exe";
        
        string pipeGuid = Guid.NewGuid().ToString(); // use this as the pipe name

        _serverProcess = new Process();
        _serverProcess.StartInfo = new ProcessStartInfo {
            FileName = unityAppPath,
            Arguments = $"-pipeName {pipeGuid}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        _serverProcess.EnableRaisingEvents = true;
        
        _serverProcess.Start();
        
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
        
        _serverProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) { LogLine(e.Data); }
        };

        _serverProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) { LogLine(e.Data); }
        };
        
        _ = Task.Run(async () => {
            try {
                _pipeServer = new NamedPipeServerStream(pipeGuid, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                LogLine("Waiting for Unity to connect...");
                
                await _pipeServer.WaitForConnectionAsync(cancellationToken);
                _pipeWriter = new StreamWriter(_pipeServer) { AutoFlush = true };
                LogLine("Unity connected to pipe.");
                
                // Read messages from Unity
                using StreamReader reader = new StreamReader(_pipeServer, leaveOpen: true);
                while (!cancellationToken.IsCancellationRequested && _pipeServer.IsConnected) {
                    string? line = await reader.ReadLineAsync(cancellationToken);
                    if (line != null) { LogLine(line); }
                }
            }
            catch (Exception ex) { LogLine($"Pipe error: {ex.Message}"); }
        }, cancellationToken);
        
        _commandInput.KeyDown += (_, e) => {
            if (e != Key.Enter) { return; }
            string command = _commandInput.Text;
            if (!string.IsNullOrWhiteSpace(command) && _pipeWriter != null) {
                try { _pipeWriter.WriteLine(command); }
                catch (Exception ex) { LogLine($"Failed to send command: {ex.Message}"); }
            }
            _commandInput.Text = string.Empty;
        };
        
        LogLine("Unity process started.");
        try { await _serverProcess.WaitForExitAsync(cancellationToken); } catch { /* Ignored */ }
        LogLine("Unity process exited.");
    }
}