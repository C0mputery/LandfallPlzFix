using System.Diagnostics;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using TwoWayAnonymousPipe;

namespace ComputeryTabgCLI;

internal static class Program {
    private static Process? _serverProcess;

    public static void Main(string[] args) {
        using IApplication app = Application.Create().Init();
        Window top = new Window();
        
        // Log view
        TextView logView = new TextView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = true,
        };
        logView.Initialized += (s, e) => { logView.ContextMenu!.Root = new Menu(); }; // Disable context menu in this dumb hacky way
        logView.DrawingText += (s, e) => { ClampLogScroll(); };
        void ClampLogScroll() {
            int lines = logView.Lines;
            int visibleHeight = logView.GetContentSize().Height;
            int maxScroll = Math.Max(0, lines - visibleHeight);
            if (logView.TopRow > maxScroll) { logView.TopRow = maxScroll; }
        }
        
        top.Add(logView);
        
        TextField commandInput = new TextField {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };
        top.Add(commandInput);
        void AppendLog(string text) {
            int currentTopRow = logView.TopRow;
            int currentLines = logView.Lines;
            int visibleHeight = logView.GetContentSize().Height;
            int maxScroll = Math.Max(0, currentLines - visibleHeight);
            bool wasAtBottom = (currentLines == 0) || (currentTopRow >= maxScroll - 1);
            
            if (!string.IsNullOrEmpty(logView.Text)) { logView.Text += "\n"; }
            logView.Text += $"{text}";
            
            if (wasAtBottom) {
                logView.MoveEnd();
                ClampLogScroll();
            } else {
                int newLines = logView.Lines;
                int newMaxScroll = Math.Max(0, newLines - visibleHeight);
                logView.TopRow = Math.Min(currentTopRow, newMaxScroll);
            }
        }
        
        
        commandInput.KeyDown += (s, e) => {
            if (e != Key.Enter) { return; }
            string command = commandInput.Text;
            string tooAdd = $"> {command}";
            AppendLog(tooAdd);
        };
        
        app.Run(top);
        
        top.Dispose();
    }

    private static async Task RunServerAsync(CancellationToken cancellationToken) {
        string unityAppPath = @"C:\Users\Computery\Desktop\LandfallPlzFix\Server\TABG.exe";
        
        using TwoWayAnonymousPipeServer pipeServer = new TwoWayAnonymousPipeServer();
        TwoWayAnonymousPipeHandles pipeHandles = pipeServer.InitializePipes();

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = unityAppPath,
            Arguments = $"-batchmode -nographics -pipeHandles {pipeHandles}",
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
                //consoleView.AppendLog($"{e.Data}");
            }
        };

        _serverProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                //consoleView.AppendLog($"{e.Data}");
            }
        };

        _serverProcess.Start();
        
        pipeServer.CloseClientHandles();

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        //consoleView.AppendLog("Unity process started.");
        await _serverProcess.WaitForExitAsync(cancellationToken);
        //consoleView.AppendLog("Unity process exited.");
    }
}