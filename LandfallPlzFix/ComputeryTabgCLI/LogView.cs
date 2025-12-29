using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;

namespace ComputeryTabgCLI;

/// <summary>
/// High-performance log view that uses a circular buffer for efficient handling of large amounts of text.
/// Supports scrolling, word wrapping, and auto-scroll to bottom.
/// </summary>
public class LogView : View {
    private readonly List<string> _lines = new();
    private readonly List<int> _lineHeights = new();
    private readonly Lock _lock = new();
    private readonly List<string> _logBuffer = new();
    private readonly Lock _logBufferLock = new();
    private int _maxLines = 10000;
    private int _topLine;
    private bool _autoScroll = true;
    private bool _wordWrap = true;
    private readonly List<string> _wrappedLines = [];
    private int _lastWidth = -1;
    private bool _needsRewrap = true;

    public LogView() {
        CanFocus = true;
        BorderStyle = LineStyle.Rounded;
        
        // Handle keyboard input for scrolling
        KeyBindings.Add(Key.PageUp, Command.PageUp);
        KeyBindings.Add(Key.PageDown, Command.PageDown);
        KeyBindings.Add(Key.Home, Command.Start);
        KeyBindings.Add(Key.End, Command.End);
        KeyBindings.Add(Key.CursorUp, Command.Up);
        KeyBindings.Add(Key.CursorDown, Command.Down);
        
        AddCommand(Command.PageUp, () => { PageUp(); return true; });
        AddCommand(Command.PageDown, () => { PageDown(); return true; });
        AddCommand(Command.Start, () => { ScrollToTop(); return true; });
        AddCommand(Command.End, () => { ScrollToBottom(); return true; });
        AddCommand(Command.Up, () => { ScrollUp(); return true; });
        AddCommand(Command.Down, () => { ScrollDown(); return true; });
    }

    /// <summary>
    /// Maximum number of lines to keep in the buffer. Older lines are discarded.
    /// </summary>
    public int MaxLines {
        get => _maxLines;
        set {
            _maxLines = Math.Max(100, value);
            TrimBuffer();
        }
    }

    /// <summary>
    /// Automatically scroll to the bottom when new lines are added.
    /// </summary>
    public bool AutoScroll {
        get => _autoScroll;
        set => _autoScroll = value;
    }

    /// <summary>
    /// Enable word wrapping for long lines.
    /// </summary>
    public bool WordWrap {
        get => _wordWrap;
        set {
            if (_wordWrap != value) {
                _wordWrap = value;
                _lastWidth = -1; // Force rewrap
                SetNeedsDraw();
            }
        }
    }

    /// <summary>
    /// Get the current number of lines in the buffer.
    /// </summary>
    public int LineCount {
        get {
            lock (_lock) {
                return _lines.Count;
            }
        }
    }

    /// <summary>
    /// Queue a line to be added to the log view (thread-safe).
    /// </summary>
    public void LogLine(string text) {
        lock (_logBufferLock) {
            _logBuffer.Add(text);
        }
    }

    /// <summary>
    /// Flush the log buffer and add all queued lines to the view.
    /// Should be called periodically from the main thread.
    /// </summary>
    public void FlushLogBuffer() {
        List<string>? linesToAdd = null;
        lock (_logBufferLock) {
            if (_logBuffer.Count > 0) {
                linesToAdd = new List<string>(_logBuffer);
                _logBuffer.Clear();
            }
        }

        if (linesToAdd != null) {
            AddLines(linesToAdd);
        }
    }

    /// <summary>
    /// Add a line to the log view.
    /// </summary>
    public void AddLine(string line) {
        AddLines(new[] { line });
    }

    /// <summary>
    /// Add multiple lines to the log view.
    /// </summary>
    public void AddLines(IEnumerable<string> lines) {
        lock (_lock) {
            if (_needsRewrap || _lastWidth <= 0) {
                _lines.AddRange(lines);
                TrimBuffer();
                _needsRewrap = true;
                SetNeedsDraw();
                return;
            }

            foreach (var line in lines) {
                _lines.Add(line);
                int height = WrapAndAddLine(line, _lastWidth);
                _lineHeights.Add(height);
            }
            
            TrimBuffer();
            
            if (_autoScroll) {
                int height = GetVisibleHeight();
                int maxScroll = Math.Max(0, _wrappedLines.Count - height);
                _topLine = maxScroll;
            }
            
            SetNeedsDraw();
        }
    }

    /// <summary>
    /// Clear all lines from the log view.
    /// </summary>
    public void Clear() {
        lock (_lock) {
            _lines.Clear();
            _wrappedLines.Clear();
            _lineHeights.Clear();
            _topLine = 0;
            _needsRewrap = false;
            _lastWidth = -1;
            SetNeedsDraw();
        }
    }

    private void TrimBuffer() {
        int excess = _lines.Count - _maxLines;
        if (excess <= 0) return;

        if (!_needsRewrap && _lineHeights.Count == _lines.Count) {
            int wrappedToRemove = 0;
            for (int i = 0; i < excess; i++) {
                wrappedToRemove += _lineHeights[i];
            }
            _lineHeights.RemoveRange(0, excess);
            _wrappedLines.RemoveRange(0, wrappedToRemove);
            _topLine = Math.Max(0, _topLine - wrappedToRemove);
        } else {
            _needsRewrap = true;
        }
        
        _lines.RemoveRange(0, excess);
    }

    private int GetVisibleHeight() {
        return Math.Max(1, Viewport.Height);
    }

    private int WrapAndAddLine(string line, int width) {
        if (string.IsNullOrEmpty(line)) {
            _wrappedLines.Add(string.Empty);
            return 1;
        }

        if (!_wordWrap || line.Length <= width) {
            _wrappedLines.Add(line);
            return 1;
        }

        int count = 0;
        ReadOnlySpan<char> span = line.AsSpan();
        for (int i = 0; i < span.Length; i += width) {
            int length = Math.Min(width, span.Length - i);
            _wrappedLines.Add(new string(span.Slice(i, length)));
            count++;
        }
        return count;
    }

    private void WrapLines(int width) {
        _wrappedLines.Clear();
        _lineHeights.Clear();
        
        // Pre-allocate estimated capacity
        _wrappedLines.EnsureCapacity(_lines.Count);
        _lineHeights.EnsureCapacity(_lines.Count);

        foreach (string line in _lines) {
            int height = WrapAndAddLine(line, width);
            _lineHeights.Add(height);
        }
    }

    protected override bool OnDrawingContent(DrawContext? context) {
        base.OnDrawingContent(context);
        
        lock (_lock) {
            int width = Viewport.Width;
            int height = Viewport.Height;

            if (height <= 0 || width <= 0) return true;

            // Rewrap only if width changed or content changed
            if (_lastWidth != width || _needsRewrap) {
                WrapLines(width);
                _lastWidth = width;
                _needsRewrap = false;
                
                // Auto-scroll to bottom after rewrap (inline to avoid lock reentry)
                if (_autoScroll) {
                    int maxScrollAfterWrap = Math.Max(0, _wrappedLines.Count - height);
                    _topLine = maxScrollAfterWrap;
                }
            }

            // Clamp scroll position
            int maxScroll = Math.Max(0, _wrappedLines.Count - height);
            _topLine = Math.Clamp(_topLine, 0, maxScroll);

            // Draw visible lines
            for (int i = 0; i < height; i++) {
                int lineIndex = _topLine + i;
                if (lineIndex >= _wrappedLines.Count) break;

                string line = _wrappedLines[lineIndex];
                Move(0, i);
                
                // Draw the line, truncating if necessary
                if (line.Length > width) {
                    AddStr(line[..width]);
                } else {
                    AddStr(line);
                }
            }
        }
        
        return true;
    }

    public void ScrollUp(int lines = 1) {
        lock (_lock) {
            _topLine = Math.Max(0, _topLine - lines);
            _autoScroll = false;
            SetNeedsDraw();
        }
    }

    public void ScrollDown(int lines = 1) {
        lock (_lock) {
            int maxScroll = Math.Max(0, _wrappedLines.Count - GetVisibleHeight());
            _topLine = Math.Min(maxScroll, _topLine + lines);
            
            // Re-enable auto-scroll if we're at the bottom
            if (_topLine >= maxScroll - 1) {
                _autoScroll = true;
            }
            
            SetNeedsDraw();
        }
    }

    public void PageUp() {
        int visibleHeight = Math.Max(1, GetVisibleHeight() - 1);
        ScrollUp(visibleHeight);
    }

    public void PageDown() {
        int visibleHeight = Math.Max(1, GetVisibleHeight() - 1);
        ScrollDown(visibleHeight);
    }

    public void ScrollToTop() {
        lock (_lock) {
            _topLine = 0;
            _autoScroll = false;
            SetNeedsDraw();
        }
    }

    public void ScrollToBottom() {
        lock (_lock) {
            int maxScroll = Math.Max(0, _wrappedLines.Count - GetVisibleHeight());
            _topLine = maxScroll;
            _autoScroll = true;
            SetNeedsDraw();
        }
    }

    protected override bool OnMouseEvent(MouseEventArgs mouseEvent) {
        if (!mouseEvent.Handled && CanFocus && !HasFocus) {
            SetFocus();
        }

        if (mouseEvent.Flags.HasFlag(MouseFlags.WheeledDown)) {
            ScrollDown();
            return true;
        }
        
        if (mouseEvent.Flags.HasFlag(MouseFlags.WheeledUp)) {
            ScrollUp();
            return true;
        }

        return base.OnMouseEvent(mouseEvent);
    }
}

