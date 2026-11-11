using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Timer2.Core.Models;
using Timer2.Core.Storage;

namespace Timer2.App.Services
{
    public sealed class ChromeActivityRecorder : IDisposable
    {
        private readonly object _sync = new object();
        private readonly ChromeActivityLog _activityLog;
        private System.Threading.Timer _pollTimer;
        private bool _isRecording;
        private bool _chromeWasForeground;
        private string _lastWindowTitle = string.Empty;
        private string _lastError;
        private bool _disposed;

        public ChromeActivityLog ActivityLog => _activityLog;

        public bool IsRecording
        {
            get
            {
                lock (_sync)
                {
                    return _isRecording;
                }
            }
        }

        public string LastError
        {
            get
            {
                lock (_sync)
                {
                    return _lastError;
                }
            }
        }

        public ChromeActivityRecorder(ChromeActivityLog activityLog)
        {
            _activityLog = activityLog ?? throw new ArgumentNullException(nameof(activityLog));
        }

        public void Start()
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_isRecording)
                {
                    return;
                }

                _activityLog.Append(new ChromeActivityEntry
                {
                    Timestamp = DateTime.Now,
                    EventType = ChromeActivityEventTypes.RecordingStarted
                });
                _chromeWasForeground = false;
                _lastWindowTitle = string.Empty;
                _lastError = null;
                _isRecording = true;
                _pollTimer = new System.Threading.Timer(PollForegroundChrome, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            }
        }

        public void Stop()
        {
            lock (_sync)
            {
                if (!_isRecording)
                {
                    return;
                }

                _isRecording = false;
                _pollTimer?.Dispose();
                _pollTimer = null;
                try
                {
                    _activityLog.Append(new ChromeActivityEntry
                    {
                        Timestamp = DateTime.Now,
                        EventType = ChromeActivityEventTypes.RecordingStopped,
                        WindowTitle = _lastWindowTitle
                    });
                    _lastError = null;
                }
                finally
                {
                    _chromeWasForeground = false;
                    _lastWindowTitle = string.Empty;
                }
            }
        }

        public bool CaptureCurrentTab()
        {
            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                string title = GetForegroundChromeTitle() ?? GetAvailableChromeWindowTitle();
                if (title == null)
                {
                    return false;
                }

                _activityLog.Append(new ChromeActivityEntry
                {
                    Timestamp = DateTime.Now,
                    EventType = ChromeActivityEventTypes.Snapshot,
                    WindowTitle = title
                });
                _lastError = null;
                return true;
            }
        }

        public void Dispose()
        {
            Stop();
            lock (_sync)
            {
                _disposed = true;
            }
        }

        private void PollForegroundChrome(object state)
        {
            lock (_sync)
            {
                if (!_isRecording || _disposed)
                {
                    return;
                }

                try
                {
                    string title = GetForegroundChromeTitle();
                    if (title == null)
                    {
                        if (_chromeWasForeground)
                        {
                            AppendTransition(ChromeActivityEventTypes.FocusLost, _lastWindowTitle);
                            _chromeWasForeground = false;
                        }
                    }
                    else if (!_chromeWasForeground)
                    {
                        AppendTransition(
                            _lastWindowTitle.Length == 0
                                ? ChromeActivityEventTypes.TabObserved
                                : ChromeActivityEventTypes.ForegroundResumed,
                            title);
                        _chromeWasForeground = true;
                        _lastWindowTitle = title;
                    }
                    else if (!string.Equals(title, _lastWindowTitle, StringComparison.Ordinal))
                    {
                        AppendTransition(IsNewTabTitle(title)
                            ? ChromeActivityEventTypes.NewTab
                            : ChromeActivityEventTypes.TabChanged, title);
                        _lastWindowTitle = title;
                    }

                    _lastError = null;
                }
                catch (Exception ex) when (
                    ex is IOException ||
                    ex is UnauthorizedAccessException ||
                    ex is Win32Exception ||
                    ex is InvalidOperationException ||
                    ex is ArgumentException)
                {
                    _lastError = ex.Message;
                }
            }
        }

        private void AppendTransition(string eventType, string title)
        {
            _activityLog.Append(new ChromeActivityEntry
            {
                Timestamp = DateTime.Now,
                EventType = eventType,
                WindowTitle = title
            });
        }

        private static bool IsNewTabTitle(string title) =>
            string.IsNullOrWhiteSpace(title) ||
            string.Equals(title, "(Untitled Chrome tab)", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(title, "New Tab", StringComparison.OrdinalIgnoreCase);

        private static string GetForegroundChromeTitle()
        {
            IntPtr window = NativeMethods.GetForegroundWindow();
            if (window == IntPtr.Zero)
            {
                return null;
            }

            NativeMethods.GetWindowThreadProcessId(window, out uint processId);
            return IsChromeProcess(processId) ? ReadWindowTitle(window) : null;
        }

        private static bool IsChromeProcess(uint processId)
        {
            if (processId == 0)
            {
                return false;
            }

            using Process process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, "chrome", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetAvailableChromeWindowTitle()
        {
            foreach (Process process in Process.GetProcessesByName("chrome").OrderByDescending(p => p.MainWindowHandle != IntPtr.Zero))
            {
                using (process)
                {
                    if (process.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    string title = ReadWindowTitle(process.MainWindowHandle);
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        return NormalizeWindowTitle(title);
                    }
                }
            }

            return null;
        }

        private static string ReadWindowTitle(IntPtr window)
        {
            int length = NativeMethods.GetWindowTextLength(window);
            var buffer = new System.Text.StringBuilder(length + 1);
            NativeMethods.GetWindowText(window, buffer, buffer.Capacity);
            return NormalizeWindowTitle(buffer.ToString());
        }

        private static string NormalizeWindowTitle(string title)
        {
            const string chromeSuffix = " - Google Chrome";
            string normalized = title?.Trim() ?? string.Empty;
            if (normalized.EndsWith(chromeSuffix, StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - chromeSuffix.Length).TrimEnd();
            }

            return normalized.Length == 0 ? "(Untitled Chrome tab)" : normalized;
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern IntPtr GetForegroundWindow();

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern int GetWindowText(IntPtr window, System.Text.StringBuilder text, int maxCount);

            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
            internal static extern int GetWindowTextLength(IntPtr window);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        }
    }
}
