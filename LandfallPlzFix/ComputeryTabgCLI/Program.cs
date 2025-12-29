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

public class PlayerListEntry(string epicUserName, VisitorInfo info) {
    public readonly string EpicUserName = epicUserName;
    public VisitorInfo Info = info;
    public override string ToString() { return $"{Info.DisplayNames[^1].Value}"; }
}

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
    
    private static View _playerView = null!;
    private static ListView _playersListView = null!;
    private static TextView _playerDetailsView = null!;
    private static readonly ObservableCollection<PlayerListEntry> Players = [];
    
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
        SetupPlayerView();
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

    private static void SetupPlayerView() {
        _playerView = new View() { 
            X = 0,
            Y = 2,
            Width = Dim.Fill(), 
            Height = Dim.Fill(), 
            Visible = false,
            SuperViewRendersLineCanvas = true,
        };
        _top.Add(_playerView);
        
        _playersListView = new ListView {
            X = 0,
            Y = 0,
            Width = 30,
            Height = Dim.Fill(),
            BorderStyle = LineStyle.Rounded,
            SuperViewRendersLineCanvas = true,
        };
        _playersListView.Border?.SetScheme(LineScheme);
        _playersListView.SetSource(Players);
        _playersListView.SelectedItemChanged += OnPlayerSelected;
        _playerView.Add(_playersListView);

        _playerDetailsView = new TextView {
            X = Pos.Right(_playersListView) - 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            BorderStyle = LineStyle.Rounded,
            SuperViewRendersLineCanvas = true,
        };
        _playerDetailsView.Border?.SetScheme(LineScheme);
        _playerView.Add(_playerDetailsView);
    }

    private static void OnPlayerSelected(object? sender, ListViewItemEventArgs e) {
        if (e.Value is PlayerListEntry entry) {
            _playerDetailsView.Text = FormatVisitorInfo(entry.EpicUserName, entry.Info);
        } else {
            _playerDetailsView.Text = string.Empty;
        }
    }

    private static string FormatVisitorInfo(string epicUserName, VisitorInfo info) {
        StringBuilder sb = new();
        sb.AppendLine($"EpicID: {epicUserName}");
        sb.AppendLine($"First Seen: {info.FirstSeen}");
        sb.AppendLine($"Last Seen: {info.LastSeen}");
        sb.AppendLine($"Permission Level: {info.PermissionLevel}");
        sb.AppendLine();
        
        sb.AppendLine("Display Names:");
        foreach(var item in info.DisplayNames) sb.AppendLine($" - {item.Value} ({item.FirstSeen} - {item.LastSeen})");
        
        sb.AppendLine("Steam IDs:");
        foreach(var item in info.SteamIds) sb.AppendLine($" - {item.Value} ({item.FirstSeen} - {item.LastSeen})");

        sb.AppendLine("Playfab IDs:");
        foreach(var item in info.PlayfabIds) sb.AppendLine($" - {item.Value} ({item.FirstSeen} - {item.LastSeen})");

        sb.AppendLine("IP Addresses:");
        foreach(var item in info.IpAddresses) sb.AppendLine($" - {item.Value} ({item.FirstSeen} - {item.LastSeen})");

        return sb.ToString();
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
            _playerView.Visible = false;
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
            _playerView.Visible = true;
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
                        
                        if (messageType == "PlayerJoined" && root.TryGetProperty("epicUserName", out JsonElement epicUserNameElement) && root.TryGetProperty("visitorInfo", out JsonElement visitorInfoElement)) {
                            string? epicUserName = epicUserNameElement.GetString();
                            if (string.IsNullOrEmpty(epicUserName)) { continue; }
                            VisitorInfo visitorInfo = JsonSerializer.Deserialize<VisitorInfo>(visitorInfoElement.GetRawText())!;
                            
                            _app.Invoke(() => {
                                PlayerListEntry? existingEntry = Players.FirstOrDefault(p => p.EpicUserName == epicUserName);
                                if (existingEntry != null) {
                                    existingEntry.Info = visitorInfo;
                                    
                                    int? savedSelection = _playersListView.SelectedItem;
                                    _playersListView.SetSource(Players);
                                    
                                    if (savedSelection is int idx && idx >= 0 && idx < Players.Count) {
                                        _playersListView.SelectedItem = idx;
                                    }
                                    
                                    if (_playersListView.SelectedItem == savedSelection && 
                                        savedSelection is int sIdx && 
                                        Players[sIdx] == existingEntry) {
                                        _playerDetailsView.Text = FormatVisitorInfo(existingEntry.EpicUserName, existingEntry.Info);
                                    }
                                } else {
                                    int? savedSelection = _playersListView.SelectedItem;
                                    Players.Add(new PlayerListEntry(epicUserName, visitorInfo));
                                    _playersListView.SetSource(Players);
                                    if (savedSelection is int idx) _playersListView.SelectedItem = idx;
                                }
                            });
                        }
                        else if (messageType == "PlayerLeft" && root.TryGetProperty("epicUserName", out JsonElement playerLeftEpicUserNameElement)) {
                            string? epicUserName = playerLeftEpicUserNameElement.GetString();
                            if (string.IsNullOrEmpty(epicUserName)) { continue; }
                            _app.Invoke(() => {
                                var entryToRemove = Players.FirstOrDefault(p => p.EpicUserName == epicUserName);
                                if (entryToRemove != null) {
                                    int index = Players.IndexOf(entryToRemove);
                                    var oldSelection = _playersListView.SelectedItem;
                                    
                                    Players.Remove(entryToRemove);
                                    _playersListView.SetSource(Players);
                                    
                                    if (oldSelection == index) {
                                        _playerDetailsView.Text = string.Empty;
                                    } else if (oldSelection is int sel && sel > index) {
                                        _playersListView.SelectedItem = Math.Max(0, sel - 1);
                                    } else if (oldSelection is int sel2) {
                                        _playersListView.SelectedItem = sel2;
                                    }
                                }
                            });
                        }
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


