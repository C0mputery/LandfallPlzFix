using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace ComputeryTabgCLI;

internal static class Program {
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    private static Process? _serverProcess;
    private static IApplication _app = null!;
    private static NamedPipeServerStream? _pipeServer;
    private static StreamWriter? _pipeWriter;
    
    private static Window _top = null!;

    private static View _serverView = null!;
    private static LogView _logView = null!;
    private static TextField _commandInput = null!;
    
    private static readonly Color AccentColor = new (0x8B, 0xE0, 0xFF);
    private static Scheme DefaultScheme => new() { };
    private static readonly Attribute LineAttr = new Attribute(AccentColor, Color.Black);
    private static readonly Scheme LineScheme = new() {
        Normal = LineAttr,
        HotNormal = LineAttr,
        Focus = LineAttr,
        HotFocus = LineAttr,
        Active = LineAttr,
        HotActive = LineAttr,
        Highlight = LineAttr,
        Disabled = LineAttr,
        Editable = LineAttr,
        ReadOnly = LineAttr,
    };


    public static void Main(string[] args) {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Console.CancelKeyPress += OnCancelKeyPress;

        _app = Application.Create().Init();
        _top = new() { BorderStyle = LineStyle.None, };
        _top.SetScheme(DefaultScheme);

        SetupServerView();
        SetupButtons();

        _ = RunServerAsync(CancellationTokenSource.Token);

        _app.Run(_top);
        _top.Dispose();
        _app.Dispose();
        CleanupServer();
    }

    private static void SetupServerView() {
        _serverView = new View() {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            SuperViewRendersLineCanvas = true,
            CanFocus = true,
        };
        _top.Add(_serverView);

        _commandInput = new TextField {
            X = 0,
            Y = Pos.AnchorEnd(3),
            Width = Dim.Fill(),
            Height = Dim.Absolute(3),
            BorderStyle = LineStyle.Rounded,
            SuperViewRendersLineCanvas = true,
        };
        _commandInput.Border?.SetScheme(LineScheme);
        _serverView.Add(_commandInput);
        
        _logView = new LogView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            WordWrap = true,
            AutoScroll = true,
            MaxLines = 10000,
            SuperViewRendersLineCanvas = true,
        };
        _logView.Border?.SetScheme(LineScheme);
        _app.AddTimeout(TimeSpan.FromMilliseconds(10), () => {
            _logView.FlushLogBuffer();
            return true;
        });
        _serverView.Add(_logView);
    }

    private static void SetupButtons() {
        ComputeryButton serverTerminal = new ComputeryButton() {
            X = 0,
            Y = 0,
            Text = "Server Terminal",
            Title = "",
            ShadowStyle = ShadowStyle.None,
            BorderStyle = LineStyle.Rounded,
            NoDecorations = true,
            SuperViewRendersLineCanvas = true,
            IsDefault = true,
        };
        serverTerminal.Border?.SetScheme(LineScheme);
        serverTerminal.Accepting += (_, e) => {
            _serverView.Visible = true;
            e.Handled = true;
        };
        _top.Add(serverTerminal);

        ComputeryButton playersButton = new ComputeryButton() {
            X = Pos.Right(serverTerminal) - 1,
            Y = 0,
            Text = "Players",
            Title = "",
            ShadowStyle = ShadowStyle.None,
            BorderStyle = LineStyle.Rounded,
            NoDecorations = true,
            SuperViewRendersLineCanvas = true,
        };
        playersButton.Border?.SetScheme(LineScheme);
        playersButton.Accepting += (_, e) => {
            _serverView.Visible = false;
            e.Handled = true;
        };
        _top.Add(playersButton);
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
        string pipeGuid = Guid.NewGuid().ToString();

        StartServerProcess(unityAppPath, pipeGuid);
        SetupProcessEventHandlers();
        
        _ = HandlePipeCommunicationAsync(pipeGuid, cancellationToken);
        SetupCommandInputHandler();

        _logView.LogLine("Unity process started.");
        try { await _serverProcess!.WaitForExitAsync(cancellationToken); } catch { /* Ignored */ }
        _logView.LogLine("Unity process exited.");
    }

    private static void StartServerProcess(string unityAppPath, string pipeGuid) {
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
    }

    private static void SetupProcessEventHandlers() {
        if (_serverProcess == null) return;

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        _serverProcess.OutputDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) { _logView.LogLine(e.Data); }
        };

        _serverProcess.ErrorDataReceived += (sender, e) => {
            if (!string.IsNullOrEmpty(e.Data)) { _logView.LogLine(e.Data); }
        };
    }

    private static async Task HandlePipeCommunicationAsync(string pipeGuid, CancellationToken cancellationToken) {
        try {
            _pipeServer = new NamedPipeServerStream(pipeGuid, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _logView.LogLine("Waiting for Unity to connect...");

            await _pipeServer.WaitForConnectionAsync(cancellationToken);
            _pipeWriter = new StreamWriter(_pipeServer) { AutoFlush = true };
            _logView.LogLine("Unity connected to pipe.");

            await ReadUnityMessagesAsync(cancellationToken);
        }
        catch (Exception ex) { _logView.LogLine($"Pipe error: {ex.Message}"); }
    }

    private static async Task ReadUnityMessagesAsync(CancellationToken cancellationToken) {
        using StreamReader reader = new StreamReader(_pipeServer!, leaveOpen: true);
        while (!cancellationToken.IsCancellationRequested && _pipeServer?.IsConnected == true) {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line != null) {
                try {
                    using JsonDocument doc = JsonDocument.Parse(line);
                    JsonElement root = doc.RootElement;
                    
                    if (root.TryGetProperty("type", out JsonElement typeElement)) {
                        string? messageType = typeElement.GetString();
                        _logView.LogLine($"Received message of type: {messageType}");
                    }
                }
                catch (Exception e) {
                    _logView.LogLine($"Failed to parse message from Unity: {line}");
                    _logView.LogLine($"Error: {e}");
                }
            }
        }
    }

    private static void SetupCommandInputHandler() {
        _commandInput.KeyDown += (_, e) => {
            if (e != Key.Enter) { return; }
            SendCommandToServer(_commandInput.Text);
            _commandInput.Text = string.Empty;
        };
    }

    private static void SendCommandToServer(string command) {
        if (!string.IsNullOrWhiteSpace(command) && _pipeWriter != null) {
            try { _pipeWriter.WriteLine(command); }
            catch (Exception ex) { _logView.LogLine($"Failed to send command: {ex.Message}"); }
        }
    }
}


