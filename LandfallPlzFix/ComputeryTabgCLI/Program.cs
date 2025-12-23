using System.Diagnostics;
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
    
    private static TextView _logView = null!;
    private static TextField _commandInput = null!;

    // TODO: optimize this
    private static void AppendLog(string text) {
        int currentTopRow = _logView.TopRow;
        int currentLines = _logView.Lines;
        int visibleHeight = _logView.GetContentSize().Height;
        int maxScroll = Math.Max(0, currentLines - visibleHeight);
        bool wasAtBottom = (currentLines == 0) || (currentTopRow >= maxScroll - 1);

        if (!string.IsNullOrEmpty(_logView.Text)) { _logView.Text += $"\n{text}"; }
        else { _logView.Text += $"{text}"; }

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
        _logView.Initialized += (s, e) => {
            // Only way I found to disable context items is in this dumb hacky way
            View deleteAll = _logView.ContextMenu!.Root!.SubViews.ElementAt(1);
            View cut = _logView.ContextMenu!.Root.SubViews.ElementAt(3);
            _logView.ContextMenu!.Root.Remove(deleteAll);
            _logView.ContextMenu!.Root.Remove(cut);

        };
        _logView.DrawingText += (s, e) => { ClampLogScroll(); };
        
        _commandInput = new TextField {
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            Height = Dim.Absolute(3),
            BorderStyle = LineStyle.Rounded
        };
        top.Add(_commandInput);
        
        top.Add(_logView);
        
        Task serverTask = RunServerAsync(CancellationTokenSource.Token);
        
        _app.Run(top);
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
        try { CancellationTokenSource.Cancel(); }
        catch { /* Ingorned */ }

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
        
        string pipeGuid = Guid.NewGuid().ToString();
        
        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = unityAppPath,
            Arguments = $"-pipeName {pipeGuid} -headless -nographics -batchmode -logFile -",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _serverProcess = new Process();
        _serverProcess.StartInfo = startInfo;
        _serverProcess.EnableRaisingEvents = true;
        _serverProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                _app.Invoke(() => { AppendLog($"{e.Data}"); });
            }
        };

        _serverProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                _app.Invoke(() => { AppendLog($"{e.Data}"); });
            }
        };
        
        _serverProcess.Start();
        
        //pipeServer.CloseClientHandles();

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();
        
        _commandInput.KeyDown += (s, e) => {
            if (e != Key.Enter) { return; }
            string command = _commandInput.Text;
            //pipeServer.SendMessage(command);
            _commandInput.Text = string.Empty;
        };
        
        _app.Invoke(() => { AppendLog("Unity process started."); });
        try { await _serverProcess.WaitForExitAsync(cancellationToken); } catch { /* Ignored */ }
        _app.Invoke(() => { AppendLog("Unity process exited."); });
    }
}