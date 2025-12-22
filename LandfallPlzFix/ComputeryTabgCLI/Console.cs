using System.Text;
using Terminal.Gui;

namespace ComputeryTabgCLI;

public class ConsoleView : View {
    private Label _statsView = null!;
    private Label _playerListView = null!;
    private TextView _logView = null!;
    private TextField _commandInput = null!;
    private readonly StringBuilder _logBuffer = new();
    private FrameView _statsFrame = null!;
    private FrameView _playerListFrame = null!;
    private FrameView _logFrame = null!;
    private bool _panesHidden;
    private const int ResponsiveWidthThreshold = 100;

    public ConsoleView() {
        CreatePlayerListPane();
        CreateLogPane();
        CreateStatsPane();
        SetupEventHandlers();
        AdjustResponsiveLayout();
    }

    private void CreatePlayerListPane() {
        _playerListFrame = new FrameView {
            X = 0,
            Y = 0,
            Width = 20,
            Height = Dim.Fill(),
            ColorScheme = Theme.WindowScheme
        };
        _playerListView = new Label {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = Theme.LogScheme
        };
        _playerListFrame.Add(_playerListView);
        Add(_playerListFrame);
    }

    private void CreateLogPane() {
        _logFrame = new FrameView {
            X = Pos.Right(_playerListFrame),
            Y = 0,
            Width = Dim.Fill() - 30,
            Height = Dim.Fill(),
            ColorScheme = Theme.WindowScheme,
            Border = new Border { BorderStyle = BorderStyle.None }
        };

        _logView = new TextView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ReadOnly = true,
            WordWrap = true,
            ColorScheme = Theme.LogScheme
        };
        _logView.ContextMenu.MenuItems = new MenuBarItem();
        _logView.DrawContent += _ => ClampLogScroll();

        _commandInput = new TextField(string.Empty) {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            ColorScheme = Theme.InputScheme
        };
        _commandInput.ContextMenu.MenuItems = new MenuBarItem();

        _logFrame.Add(_logView);
        _logFrame.Add(_commandInput);
        Add(_logFrame);
    }

    private void CreateStatsPane() {
        _statsFrame = new FrameView {
            X = Pos.Right(_logFrame),
            Y = 0,
            Width = 30,
            Height = Dim.Fill(),
            ColorScheme = Theme.WindowScheme
        };
        _statsView = new Label {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = Theme.LogScheme,
            TextAlignment = TextAlignment.Right
        };
        _statsFrame.Add(_statsView);
        Add(_statsFrame);
    }

    private void SetupEventHandlers() {
        _commandInput.KeyPress += e => {
            if (e.KeyEvent.Key != Key.Enter) { return; }
            SendCommand(_commandInput.Text.ToString());
            _commandInput.Text = string.Empty;
            e.Handled = true;
        };

        Application.Top.Resized += _ => AdjustResponsiveLayout();
    }
    
    private void AdjustResponsiveLayout() {
        int width = Bounds.Width > 0 ? Bounds.Width : Application.Top?.Bounds.Width ?? 0;
        bool shouldHide = width < ResponsiveWidthThreshold;
            
        if (shouldHide == _panesHidden) return;
            
        _panesHidden = shouldHide;
        _statsFrame.Visible = !shouldHide;
        _playerListFrame.Visible = !shouldHide;
            
        if (shouldHide) {
            _logFrame.X = 0;
            _logFrame.Width = Dim.Fill();
        } else {
            _logFrame.X = Pos.Right(_playerListFrame);
            _logFrame.Width = Dim.Fill() - 30;
        }
            
        LayoutSubviews();
        SetNeedsDisplay();
    }

    private void ClampLogScroll() {
        int lines = _logView.Lines;
        int visibleHeight = _logView.Bounds.Height;
        int maxScroll = Math.Max(0, lines - visibleHeight);
            
        if (_logView.TopRow > maxScroll) {
            _logView.TopRow = maxScroll;
        }
    }

    public void AppendLog(string text) {
        if (_logBuffer.Length > 0) { _logBuffer.AppendLine(); }
        _logBuffer.Append(text);
            
        int currentTopRow = _logView.TopRow;
        int currentLines = _logView.Lines;
        int visibleHeight = _logView.Bounds.Height;
        int maxScroll = Math.Max(0, currentLines - visibleHeight);
        bool wasAtBottom = (currentLines == 0) || (currentTopRow >= maxScroll - 1);
            
        _logView.Text = _logBuffer.ToString();
            
        if (wasAtBottom) {
            _logView.MoveEnd();
            ClampLogScroll();
        } else {
            int newLines = _logView.Lines;
            int newMaxScroll = Math.Max(0, newLines - visibleHeight);
            _logView.TopRow = Math.Min(currentTopRow, newMaxScroll);
        }
    }
    
    public void UpdateStats(string text) {
        _statsView.Text = text;
    }

    public void UpdatePlayerList(string text) {
        _playerListView.Text = text;
    }
        
    private void SendCommand(string? cmd) { cmd ??= string.Empty; }
}