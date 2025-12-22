using System.Diagnostics;
using ComputeryTabgCLI;
using Terminal.Gui;

internal class Program {
    private static Process? _serverProcess;
    private static CancellationTokenSource? _cancellationTokenSource;

    public static void Main(string[] args) {
        Application.Init();

        Window mainWindow = new Window() {
            Title = string.Empty,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = Theme.WindowScheme,
            Border = new Border { BorderStyle = BorderStyle.None }
        };

        ConsoleView consoleView = new ConsoleView() {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        mainWindow.Add(consoleView);

        Application.Top.Add(mainWindow);

        _cancellationTokenSource = new CancellationTokenSource();
        _ = RunServerAsync(consoleView, _cancellationTokenSource.Token);

        try { Application.Run(); } 
        finally { 
            CloseServer();
            Application.Shutdown(); 
        }
    }

    private static async Task RunServerAsync(ConsoleView consoleView, CancellationToken cancellationToken) {
        string unityAppPath = @"C:\Users\Computery\Desktop\LandfallPlzFix\Server\TABG.exe";

        ProcessStartInfo startInfo = new ProcessStartInfo {
            FileName = unityAppPath,
            Arguments = "-batchmode -nographics -logFile -",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _serverProcess = new Process();
        _serverProcess.StartInfo = startInfo;
        _serverProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                consoleView.AppendLog($"[Unity]: {e.Data}");
            }
        };

        _serverProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) {
                consoleView.AppendLog($"[Unity]: {e.Data}");
            }
        };

        _serverProcess.Start();

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        consoleView.AppendLog("Unity process started.");
        await _serverProcess.WaitForExitAsync(cancellationToken);
        consoleView.AppendLog("Unity process exited.");
    }

    private static void CloseServer() {
        _cancellationTokenSource?.Cancel();
        
        if (_serverProcess != null) {
            try { if (!_serverProcess.HasExited) { _serverProcess.Kill(); } }
            finally { _serverProcess.Dispose(); }
        }

        _cancellationTokenSource?.Dispose();
    }
}