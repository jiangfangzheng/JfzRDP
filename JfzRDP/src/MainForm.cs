using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using JfzRdp.Controls;
using JfzRdp.Models;

namespace JfzRdp
{
    public sealed class MainForm : Form
    {
        private const string RemoteTabPrefix = "\U0001F5A5";

        private readonly AppConfig _config;
        private readonly Dictionary<string, TabPage> _connectionTabs =
            new Dictionary<string, TabPage>(StringComparer.OrdinalIgnoreCase);

        private MiddleClickTabControl _tabs;
        private TabPage _homePage;
        private TextBox _manualServerTextBox;
        private TextBox _manualUserNameTextBox;
        private TextBox _manualPasswordTextBox;
        private NumericUpDown _manualPortNumeric;
        private ListBox _serverListBox;
        private Label _modeInfoLabel;
        private Label _serverValueLabel;
        private Label _userValueLabel;
        private Label _passwordValueLabel;
        private Label _portValueLabel;

        private bool _tabBleedInitialized;
        private int _tabPageInsetLeft;
        private int _tabPageInsetRight;
        private int _tabPageInsetBottom;

        private bool _allowClose;

        public MainForm(AppConfig config)
        {
            _config = config ?? AppConfig.CreateDefault();

            InitializeWindow();
            BuildTabs();
            BuildHomePage();

            _tabs.SelectedTab = _homePage;
        }

        private void InitializeWindow()
        {
            AutoScaleMode = AutoScaleMode.None;
            Font = SystemFonts.MessageBoxFont;
            Text = "异想家RDP远程桌面管理 v2.0";
            StartPosition = FormStartPosition.CenterScreen;

            try
            {
                var appIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    Icon = appIcon;
                }
            }
            catch
            {
                // 某些受限环境下无法从 exe 提取图标时保持系统默认图标。
            }

            if (_config.Maximized)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                MaximizeBox = true;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                ClientSize = new Size(_config.Width + 32, _config.Height + 80);
            }
        }

        private void BuildTabs()
        {
            _tabs = new MiddleClickTabControl
            {
                Dock = DockStyle.Fill,
                Name = "MainTabs"
            };
            _tabs.TabMiddleClicked += Tabs_TabMiddleClicked;
            _tabs.SelectedIndexChanged += Tabs_SelectedIndexChanged;
            Controls.Add(_tabs);

            _homePage = new TabPage("Home")
            {
                Name = "HomeTab",
                Padding = Padding.Empty,
                UseVisualStyleBackColor = true
            };
            _tabs.TabPages.Add(_homePage);
        }

        private void BuildHomePage()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12),
                BackColor = SystemColors.Control
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _homePage.Controls.Add(root);

            var manualGroup = BuildManualGroup();
            manualGroup.Margin = new Padding(0, 0, 0, 10);
            root.Controls.Add(manualGroup, 0, 0);

            var savedGroup = BuildSavedGroup();
            savedGroup.Margin = new Padding(0);
            root.Controls.Add(savedGroup, 0, 1);
        }

        private GroupBox BuildManualGroup()
        {
            var group = new GroupBox
            {
                Text = "手动连接",
                Dock = DockStyle.Fill,
                Name = "ManualGroup"
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 10,
                RowCount = 2,
                Padding = new Padding(12, 24, 12, 10)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            group.Controls.Add(layout);

            layout.Controls.Add(CreateLabel("服务器："), 0, 0);
            _manualServerTextBox = CreateTextBox("ManualServerTextBox", "192.168.233.254");
            layout.Controls.Add(_manualServerTextBox, 1, 0);

            layout.Controls.Add(CreateLabel("账号："), 2, 0);
            _manualUserNameTextBox = CreateTextBox("ManualUserNameTextBox", "Sandeepin");
            layout.Controls.Add(_manualUserNameTextBox, 3, 0);

            layout.Controls.Add(CreateLabel("密码："), 4, 0);
            _manualPasswordTextBox = CreateTextBox("ManualPasswordTextBox", string.Empty);
            _manualPasswordTextBox.UseSystemPasswordChar = true;
            layout.Controls.Add(_manualPasswordTextBox, 5, 0);

            layout.Controls.Add(CreateLabel("端口："), 6, 0);
            _manualPortNumeric = new NumericUpDown
            {
                Name = "ManualPortNumeric",
                Minimum = 1,
                Maximum = 65535,
                Value = 3389,
                Width = 100,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 0, 5),
                ThousandsSeparator = false
            };
            layout.Controls.Add(_manualPortNumeric, 7, 0);

            var linkButton = new Button
            {
                Name = "LinkButton",
                Text = "Link",
                Width = 100,
                Height = 23,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(20, 5, 0, 5)
            };
            linkButton.Click += LinkButton_Click;
            layout.Controls.Add(linkButton, 8, 0);

            return group;
        }

        private GroupBox BuildSavedGroup()
        {
            var group = new GroupBox
            {
                Text = "保存的服务器列表",
                Dock = DockStyle.Fill,
                Name = "SavedServersGroup"
            };

            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(12, 24, 12, 12)
            };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            group.Controls.Add(outer);

            _modeInfoLabel = new Label
            {
                Name = "ModeInfoLabel",
                AutoSize = true,
                Text = FormatModeInfo(),
                Margin = new Padding(0, 0, 0, 8)
            };
            outer.Controls.Add(_modeInfoLabel, 0, 0);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            outer.Controls.Add(content, 0, 1);

            _serverListBox = new ListBox
            {
                Name = "ServerListBox",
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                DisplayMember = "ServerName",
                SelectionMode = SelectionMode.One,
                Margin = new Padding(0, 0, 12, 0)
            };
            _serverListBox.SelectedIndexChanged += ServerListBox_SelectedIndexChanged;
            _serverListBox.MouseDoubleClick += ServerListBox_MouseDoubleClick;
            content.Controls.Add(_serverListBox, 0, 0);

            var parameterGroup = new GroupBox
            {
                Text = "参数信息",
                Dock = DockStyle.Fill,
                Name = "ParameterGroup"
            };
            content.Controls.Add(parameterGroup, 1, 0);

            var parameterLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(14, 24, 14, 12)
            };
            parameterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (var i = 0; i < 4; i++)
            {
                parameterLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            }
            parameterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            _serverValueLabel = CreateValueLabel("ServerValueLabel");
            _userValueLabel = CreateValueLabel("UserNameValueLabel");
            _passwordValueLabel = CreateValueLabel("PasswordValueLabel");
            _portValueLabel = CreateValueLabel("PortValueLabel");

            parameterLayout.Controls.Add(_serverValueLabel, 0, 0);
            parameterLayout.Controls.Add(_userValueLabel, 0, 1);
            parameterLayout.Controls.Add(_passwordValueLabel, 0, 2);
            parameterLayout.Controls.Add(_portValueLabel, 0, 3);
            parameterGroup.Controls.Add(parameterLayout);

            _serverListBox.DisplayMember = "ServerName";
            foreach (var server in _config.ServerList)
            {
                _serverListBox.Items.Add(server);
            }

            if (_serverListBox.Items.Count > 0)
            {
                _serverListBox.SelectedIndex = 0;
            }
            else
            {
                UpdateSelectedServerInfo();
            }

            return group;
        }

        private string FormatModeInfo()
        {
            return "最大化：" + (_config.Maximized ? "是" : "否")
                + "    宽度：" + _config.Width
                + "    高度：" + _config.Height;
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
        }

        private static TextBox CreateTextBox(string name, string text)
        {
            return new TextBox
            {
                Name = name,
                Text = text,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 5, 8, 5)
            };
        }

        private static Label CreateValueLabel(string name)
        {
            return new Label
            {
                Name = name,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Margin = new Padding(0)
            };
        }

        private void LinkButton_Click(object sender, EventArgs e)
        {
            var server = _manualServerTextBox.Text.Trim();
            if (server.Length == 0)
            {
                MessageBox.Show(
                    this,
                    "请输入服务器地址。",
                    "手动连接",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _manualServerTextBox.Focus();
                return;
            }

            var userName = _manualUserNameTextBox.Text.Trim();
            var port = (int)_manualPortNumeric.Value;

            OpenConnection(new ConnectionInfo
            {
                ConnectionKey = BuildManualConnectionKey(server, userName, port),
                TabTitle = server,
                Server = server,
                UserName = userName,
                Password = _manualPasswordTextBox.Text,
                Port = port
            });
        }

        private void ServerListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var index = _serverListBox.IndexFromPoint(e.Location);
            if (index < 0 || index >= _serverListBox.Items.Count)
            {
                return;
            }

            var server = _serverListBox.Items[index] as ServerEntry;
            if (server == null)
            {
                return;
            }

            OpenConnection(new ConnectionInfo
            {
                ConnectionKey = "saved|" + (server.ServerName ?? server.Server ?? string.Empty).Trim(),
                TabTitle = server.ServerName,
                Server = server.Server,
                UserName = server.UserName,
                Password = server.Password,
                Port = server.Port
            });
        }

        private static string BuildManualConnectionKey(string server, string userName, int port)
        {
            return "manual|" + (server ?? string.Empty).Trim()
                + "|" + (userName ?? string.Empty).Trim()
                + "|" + port;
        }

        private void ServerListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSelectedServerInfo();
        }

        private void UpdateSelectedServerInfo()
        {
            var server = _serverListBox.SelectedItem as ServerEntry;
            if (server == null)
            {
                _serverValueLabel.Text = "服务器：";
                _userValueLabel.Text = "账号：";
                _passwordValueLabel.Text = "密码：";
                _portValueLabel.Text = "端口：";
                return;
            }

            _serverValueLabel.Text = "服务器：" + SingleLine(server.Server);
            _userValueLabel.Text = "账号：" + SingleLine(server.UserName);
            _passwordValueLabel.Text = "密码：" + SingleLine(server.Password);
            _portValueLabel.Text = "端口：" + server.Port;
        }

        private static string SingleLine(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
        }

        private void OpenConnection(ConnectionInfo connection)
        {
            if (string.IsNullOrWhiteSpace(connection.Server))
            {
                MessageBox.Show(
                    this,
                    "服务器地址不能为空。",
                    "连接",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var connectionKey = connection.ConnectionKey;
            if (!string.IsNullOrEmpty(connectionKey))
            {
                TabPage existingPage;
                if (_connectionTabs.TryGetValue(connectionKey, out existingPage))
                {
                    if (existingPage != null
                        && !existingPage.IsDisposed
                        && _tabs.TabPages.Contains(existingPage))
                    {
                        _tabs.SelectedTab = existingPage;
                        existingPage.Focus();
                        return;
                    }

                    _connectionTabs.Remove(connectionKey);
                }
            }

            var title = string.IsNullOrWhiteSpace(connection.TabTitle)
                ? connection.Server
                : connection.TabTitle;
            connection.TabTitle = title;

            var page = new TabPage(title)
            {
                Padding = Padding.Empty,
                UseVisualStyleBackColor = false,
                BackColor = Color.Black
            };

            var rdpTab = new RdpTab(
                connection,
                _config.Maximized,
                _config.Width,
                _config.Height)
            {
                Dock = DockStyle.Fill
            };

            page.Controls.Add(rdpTab);
            page.Tag = rdpTab;
            if (!string.IsNullOrEmpty(connectionKey))
            {
                _connectionTabs[connectionKey] = page;
            }

            _tabs.TabPages.Add(page);
            _tabs.SelectedTab = page;

            BeginInvoke(new Action(() =>
            {
                if (!IsDisposed && !rdpTab.IsDisposed)
                {
                    rdpTab.Start();
                }
            }));
        }

        private void Tabs_TabMiddleClicked(TabPage page)
        {
            if (page == null || page == _homePage || !_tabs.TabPages.Contains(page))
            {
                return;
            }

            ConfirmAndCloseTab(page);
        }

        private void Tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_tabs == null || _homePage == null)
            {
                return;
            }

            foreach (TabPage page in _tabs.TabPages)
            {
                if (page == _homePage)
                {
                    page.Text = "Home";
                    continue;
                }

                var rdpTab = page.Tag as RdpTab;
                if (rdpTab == null || string.IsNullOrEmpty(rdpTab.TabTitle))
                {
                    continue;
                }

                page.Text = page == _tabs.SelectedTab
                    ? RemoteTabPrefix + rdpTab.TabTitle
                    : rdpTab.TabTitle;
            }
        }

        private void ConfirmAndCloseTab(TabPage page)
        {
            var rdpTab = page.Tag as RdpTab;
            var displayName = rdpTab != null && !string.IsNullOrEmpty(rdpTab.TabTitle)
                ? rdpTab.TabTitle
                : page.Text;

            var result = MessageBox.Show(
                this,
                "确定要断开并关闭“" + displayName + "”的 RDP 连接吗？",
                "关闭 RDP 标签页",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                return;
            }

            if (rdpTab != null)
            {
                if (!string.IsNullOrEmpty(rdpTab.ConnectionKey))
                {
                    _connectionTabs.Remove(rdpTab.ConnectionKey);
                }

                rdpTab.CloseConnection();
            }

            _tabs.TabPages.Remove(page);
            page.Dispose();
        }

        private void CloseAllConnections()
        {
            for (var i = _tabs.TabPages.Count - 1; i >= 1; i--)
            {
                var rdpTab = _tabs.TabPages[i].Tag as RdpTab;
                if (rdpTab != null)
                {
                    rdpTab.CloseConnection();
                }
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            InitializeTabBleedLayout();

            if (_config.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FitFixedWindowToRemoteSize();
            }
        }

        private void InitializeTabBleedLayout()
        {
            if (_tabBleedInitialized)
            {
                return;
            }

            PerformLayout();

            _tabPageInsetLeft = Math.Max(0, _homePage.Left);
            _tabPageInsetRight = Math.Max(0, _tabs.ClientSize.Width - _homePage.Right);
            _tabPageInsetBottom = Math.Max(0, _tabs.ClientSize.Height - _homePage.Bottom);

            // 原生 TabControl 会在页面四周绘制 4px 左右的边框。把 TabControl 向左/下扩展，
            // 让页面内容和窗口客户区边缘对齐，从而去掉 RDP 画面与窗口边框之间的缝隙。
            _tabs.Dock = DockStyle.None;
            _tabBleedInitialized = true;
            ApplyTabBleedBounds();
        }

        private void ApplyTabBleedBounds()
        {
            if (!_tabBleedInitialized || _tabs == null || !_tabs.IsHandleCreated)
            {
                return;
            }

            _tabs.Bounds = new Rectangle(
                -_tabPageInsetLeft,
                0,
                Math.Max(1, ClientSize.Width + _tabPageInsetLeft + _tabPageInsetRight),
                Math.Max(1, ClientSize.Height + _tabPageInsetBottom));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ApplyTabBleedBounds();
        }

        private void FitFixedWindowToRemoteSize()
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                PerformLayout();
                var pageSize = _homePage.ClientSize;
                var deltaWidth = _config.Width - pageSize.Width;
                var deltaHeight = _config.Height - pageSize.Height;

                if (deltaWidth == 0 && deltaHeight == 0)
                {
                    break;
                }

                var nextWidth = ClientSize.Width + deltaWidth;
                var nextHeight = ClientSize.Height + deltaHeight;
                if (nextWidth < 200 || nextHeight < 200)
                {
                    break;
                }

                ClientSize = new Size(nextWidth, nextHeight);
            }

            MinimumSize = Size;
            MaximumSize = Size;

            var workingArea = Screen.FromControl(this).WorkingArea;
            var x = workingArea.Left + Math.Max(0, (workingArea.Width - Width) / 2);
            var y = workingArea.Top + Math.Max(0, (workingArea.Height - Height) / 2);
            Location = new Point(x, y);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    this,
                    "确定要关闭 JfzRDP 吗？\r\n所有 RDP 连接都将断开。",
                    "退出确认",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    base.OnFormClosing(e);
                    return;
                }
            }

            _allowClose = true;
            CloseAllConnections();
            base.OnFormClosing(e);
        }
    }
}
