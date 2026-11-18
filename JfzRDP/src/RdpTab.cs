using System;
using System.Drawing;
using System.Windows.Forms;
using AxMSTSCLib;
using JfzRdp.Models;

namespace JfzRdp
{
    /// <summary>
    /// 一个标签页内的 RDP 会话。使用 AxInterop.MSTSCLib / Interop.MSTSCLib 承载 mstscax.dll。
    /// </summary>
    public sealed class RdpTab : UserControl
    {
        private const int ConnectTimeoutMilliseconds = 20000;

        private readonly ConnectionInfo _connection;
        private readonly bool _maximized;
        private readonly int _configuredWidth;
        private readonly int _configuredHeight;
        private readonly AxMsRdpClient10NotSafeForScripting _rdp;
        private readonly Timer _watchdogTimer;
        private readonly Timer _resizeTimer;

        private bool _started;
        private bool _closeRequested;
        private bool _disposed;

        public RdpTab(ConnectionInfo connection, bool maximized, int configuredWidth, int configuredHeight)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _maximized = maximized;
            _configuredWidth = configuredWidth;
            _configuredHeight = configuredHeight;

            Dock = DockStyle.Fill;
            BackColor = Color.Black;

            _rdp = new AxMsRdpClient10NotSafeForScripting
            {
                Dock = DockStyle.Fill,
                Name = "RdpClient"
            };
            Controls.Add(_rdp);

            _rdp.OnConnected += Rdp_OnConnected;
            _rdp.OnLoginComplete += Rdp_OnLoginComplete;
            _rdp.OnDisconnected += Rdp_OnDisconnected;
            _rdp.OnFatalError += Rdp_OnFatalError;

            _watchdogTimer = new Timer { Interval = ConnectTimeoutMilliseconds };
            _watchdogTimer.Tick += WatchdogTimer_Tick;

            _resizeTimer = new Timer { Interval = 500 };
            _resizeTimer.Tick += ResizeTimer_Tick;
        }

        public string ConnectionKey
        {
            get { return _connection == null ? string.Empty : _connection.ConnectionKey; }
        }

        public string TabTitle
        {
            get { return _connection == null ? string.Empty : _connection.TabTitle; }
        }

        public void Start()
        {
            if (_started || _closeRequested || _disposed)
            {
                return;
            }

            _started = true;

            try
            {
                if (!_rdp.IsHandleCreated)
                {
                    _rdp.HandleCreated += Rdp_HandleCreated;
                    CreateControl();
                    _rdp.CreateControl();
                }
            }
            catch (Exception ex)
            {
                ShowConnectError("无法初始化 RDP 控件。\r\n\r\n" + ex.Message);
                return;
            }

            if (_rdp.IsHandleCreated)
            {
                _rdp.HandleCreated -= Rdp_HandleCreated;
                ConnectCore();
            }
        }

        public void CloseConnection()
        {
            if (_closeRequested)
            {
                return;
            }

            _closeRequested = true;
            StopTimers();

            try
            {
                if (_rdp.IsHandleCreated)
                {
                    _rdp.Disconnect();
                }
            }
            catch
            {
                // 已断开或正在连接时，Disconnect 可能抛出 COM 异常；关闭流程不应被阻塞。
            }
        }

        private void Rdp_HandleCreated(object sender, EventArgs e)
        {
            _rdp.HandleCreated -= Rdp_HandleCreated;
            if (_closeRequested || _disposed)
            {
                return;
            }

            if (IsHandleCreated)
            {
                BeginInvoke(new Action(ConnectCore));
            }
            else
            {
                ConnectCore();
            }
        }

        private void ConnectCore()
        {
            if (_closeRequested || _disposed)
            {
                return;
            }

            try
            {
                ConfigureClient();
                _rdp.Connect();

                _watchdogTimer.Stop();
                _watchdogTimer.Start();
            }
            catch (Exception ex)
            {
                _watchdogTimer.Stop();
                _rdp.DisconnectedText = "连接失败";
                ShowConnectError("无法发起 RDP 连接。\r\n\r\n" + ex.Message);
            }
        }

        private void ConfigureClient()
        {
            var targetSize = GetTargetSize();

            _rdp.Server = _connection.Server;
            _rdp.UserName = _connection.UserName ?? string.Empty;
            _rdp.Domain = string.Empty;
            _rdp.ColorDepth = 32;
            _rdp.FullScreen = false;
            _rdp.DesktopWidth = targetSize.Width;
            _rdp.DesktopHeight = targetSize.Height;
            _rdp.ConnectingText = "正在连接...";
            _rdp.DisconnectedText = "连接已断开";
            _rdp.AdvancedSettings2.RDPPort = _connection.Port;
            _rdp.AdvancedSettings2.ClearTextPassword = _connection.Password ?? string.Empty;
            _rdp.AdvancedSettings2.SmartSizing = _maximized;
            _rdp.AdvancedSettings3.EnableAutoReconnect = false; // 禁止 ActiveX 内置自动重连

            // 这些设置位于较新的接口上；RDP 8.1 及以上系统均可用。
            _rdp.AdvancedSettings9.RedirectClipboard = true;
            _rdp.AdvancedSettings9.RedirectDrives = false;
            _rdp.AdvancedSettings9.AuthenticationLevel = 0;
            _rdp.AdvancedSettings9.EnableCredSspSupport = true;
            _rdp.AdvancedSettings9.PerformanceFlags = 0;
        }

        private Size GetTargetSize()
        {
            if (!_maximized)
            {
                return new Size(
                    Math.Max(320, _configuredWidth),
                    Math.Max(240, _configuredHeight));
            }

            var size = _rdp.ClientSize;
            if (size.Width < 200 || size.Height < 200)
            {
                size = new Size(1024, 768);
            }

            return size;
        }

        private void Rdp_OnConnected(object sender, EventArgs e)
        {
            if (_closeRequested || _disposed)
            {
                return;
            }

            _watchdogTimer.Stop();
            _rdp.DisconnectedText = "连接已断开";

            if (_maximized)
            {
                QueueSessionResize();
            }
        }

        private void Rdp_OnLoginComplete(object sender, EventArgs e)
        {
            if (!_closeRequested && !_disposed && _maximized)
            {
                QueueSessionResize();
            }
        }

        private void Rdp_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            if (_closeRequested || _disposed)
            {
                return;
            }

            // 意外断线后不自动重连，避免抢占远程电脑前真实用户的会话。
            _rdp.DisconnectedText = "连接已断开";
        }

        private void Rdp_OnFatalError(object sender, IMsTscAxEvents_OnFatalErrorEvent e)
        {
            if (_closeRequested || _disposed)
            {
                return;
            }

            _rdp.DisconnectedText = "连接失败";
        }

        private void WatchdogTimer_Tick(object sender, EventArgs e)
        {
            _watchdogTimer.Stop();

            if (_closeRequested || _disposed || _rdp.Connected != 0)
            {
                return;
            }

            _rdp.DisconnectedText = "连接超时";
        }

        private void QueueSessionResize()
        {
            _resizeTimer.Stop();
            _resizeTimer.Start();
        }

        private void ResizeTimer_Tick(object sender, EventArgs e)
        {
            _resizeTimer.Stop();

            if (_closeRequested || _disposed || !_maximized)
            {
                return;
            }

            var size = GetTargetSize();
            if (size.Width < 200 || size.Height < 200)
            {
                return;
            }

            try
            {
                if (_rdp.Connected == 0)
                {
                    _rdp.DesktopWidth = size.Width;
                    _rdp.DesktopHeight = size.Height;
                    return;
                }

                // 让远程桌面的分辨率跟随标签页大小；失败时 SmartSizing 仍会铺满控件。
                _rdp.UpdateSessionDisplaySettings(
                    (uint)size.Width,
                    (uint)size.Height,
                    (uint)size.Width,
                    (uint)size.Height,
                    0,
                    100,
                    100);
            }
            catch
            {
                // 某些服务器/会话不支持动态调整分辨率，由 SmartSizing 兜底。
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_maximized && _started && !_closeRequested && !_disposed)
            {
                QueueSessionResize();
            }
        }

        private void StopTimers()
        {
            _watchdogTimer.Stop();
            _resizeTimer.Stop();
        }

        private void ShowConnectError(string message)
        {
            if (_closeRequested || _disposed)
            {
                return;
            }

            MessageBox.Show(
                this,
                message,
                "RDP 连接",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposed = true;
                _closeRequested = true;
                StopTimers();

                _watchdogTimer.Dispose();
                _resizeTimer.Dispose();

                _rdp.OnConnected -= Rdp_OnConnected;
                _rdp.OnLoginComplete -= Rdp_OnLoginComplete;
                _rdp.OnDisconnected -= Rdp_OnDisconnected;
                _rdp.OnFatalError -= Rdp_OnFatalError;

                try
                {
                    if (_rdp.IsHandleCreated)
                    {
                        _rdp.Disconnect();
                    }
                }
                catch
                {
                }

                try
                {
                    _rdp.Dispose();
                }
                catch
                {
                }
            }

            base.Dispose(disposing);
        }
    }
}
