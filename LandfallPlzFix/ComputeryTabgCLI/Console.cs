/*using System.Text;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drivers;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace ComputeryTabgCLI;

public class ConsoleView : View {
    private TextView _logView = null!;
    private TextField _commandInput = null!;
    private readonly StringBuilder _logBuffer = new();
    private FrameView _logFrame = null!;

    public ConsoleView() { CreateLogPane(); }

    private void CreateLogPane() {
        _logFrame = new FrameView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };

        _logView = new TextView {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ReadOnly = true,
            WordWrap = true,
        };

        _commandInput = new TextField() {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };

        _logFrame.Add(_logView);
        _logFrame.Add(_commandInput);
        Add(_logFrame);
    }

    private void ClampLogScroll() {
        int lines = _logView.Lines;
        int  = _logView.Bounds.Height;
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
        int visibleHeight = _logView.Height;
        int maxScroll = Math.Max(0, currentLines - visibleHeight);
        bool wasAtBottom = (currentLines == 0) || (currentTopRow >= maxScroll);
            
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
        
    private void SendCommand(string? cmd) { cmd ??= string.Empty; }
}*/