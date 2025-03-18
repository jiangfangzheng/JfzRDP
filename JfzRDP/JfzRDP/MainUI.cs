using AxMSTSCLib;
using MSTSCLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Diagnostics;

namespace JfzRDP
{
    public partial class MainUI : Form
    {
        private static string version = "1.1";
        private string title = "异想家RDP远程桌面管理 v" + version;

        // 服务器配置列表
        private ServerSettings serverSettings = new ServerSettings();
        private List<JRdpObj> JRdpObjList = new List<JRdpObj>();

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        // Windows API 函数
        private const int WM_SYSCOMMAND = 0x0112;
        private const int MF_STRING = 0x0000;
        private const int MF_SEPARATOR = 0x0800;
        // 自定义菜单项的第一个ID
        private const int firstCustomID = 1000;
        // 自定义菜单项的 断开连接 ID
        private int disconnectID = -1;
        // 当前显示的RPD数组的下标
        private int currentShowIndex = -1;

        private int deltaWidth = -1;
        private int deltaHeight = -1;

        public MainUI()
        {
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;

            // 获取mstscax.dll版本
            string dllPath = Path.Combine(Environment.SystemDirectory, "mstscax.dll");
            if (File.Exists(dllPath))
            {
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(dllPath);
                Trace.WriteLine($"mstscax.dll 版本: {fileVersion.FileVersion}");
            }
            else
            {
                Trace.WriteLine("未找到 mstscax.dll");
            }

            InitializeComponent();
            this.Text = title;
            // 载入服务器配置文件
            LoadServerLists();
            // 添加自定义菜单项
            AddCustomMenuItems();
            // 订阅 FormClosing 事件
            this.FormClosing += MainForm_FormClosing;
        }

        private void LoadServerLists()
        {
            // 从json配置获取服务器信息
            string filePath = Path.Combine(AppContext.BaseDirectory, "server.json");
            if (!File.Exists(filePath))
            {
                // 如果文件不存在则创建
                CreateDefaultServerFile(filePath);
            }
            string jsonContent = File.ReadAllText(filePath);
            this.serverSettings = JsonSerializer.Deserialize<ServerSettings>(jsonContent);
            // 创建rdp控件集合
            for (int i = 0; i < this.serverSettings.ServerList.Count; ++i)
            {
                JRdpObj jRdpObj = new JRdpObj(this.serverSettings.ServerList[i], title);
                // 绑定事件
                jRdpObj.RdpClient.OnDisconnected += RdpClient_OnDisconnected;
                // 索引指定
                jRdpObj.Index = i;
                JRdpObjList.Add(jRdpObj);
            }

        }

        private void CreateDefaultServerFile(string filePath)
        {
            try
            {
                // 定义对象
                var server1 = new ServerSetting
                {
                    ServerName = "Jfz-Server1",
                    Server = "192.168.1.1",
                    UserName = "abc",
                    Password = "1",
                    Port = 3389,
                    Width = 1920,
                    Height = 1080
                };
                var server2 = new ServerSetting
                {
                    ServerName = "异想家-Server2",
                    Server = "192.168.1.2",
                    UserName = "xyz",
                    Password = "1",
                    Port = 3389,
                    Width = 1280,
                    Height = 720
                };
                var serverSettingsObj = new ServerSettings();
                serverSettingsObj.ServerList.Add(server1);
                serverSettingsObj.ServerList.Add(server2);
                // 将对象序列化为 JSON 字符串
                var options = new JsonSerializerOptions { WriteIndented = true };
                string serverSettingsStr = JsonSerializer.Serialize(serverSettingsObj, options);
                // 创建文件并写入字符串
                File.WriteAllText(filePath, serverSettingsStr, Encoding.UTF8);
                MessageBox.Show("已在程序目录下默认生成server.json，请修改为您的服务器信息！", "您似乎是第一次使用本软件或server.json文件丢失");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"创建文件时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 弹出确认框，默认选择“否”
            DialogResult result = MessageBox.Show(
                "确定要关闭窗口吗？",
                "关闭确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2 // 将默认按钮设置为“否”
            );

            // 如果用户选择“否”，则取消关闭操作
            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
        }

        private void AddCustomMenuItems()
        {
            // 获取系统菜单句柄
            IntPtr systemMenuHandle = GetSystemMenu(this.Handle, false);
            // 添加分隔符
            AppendMenu(systemMenuHandle, MF_SEPARATOR, 0, string.Empty);
            // 添加服务器列表
            int i = 0;
            foreach (ServerSetting server in serverSettings.ServerList)
            {
                AppendMenu(systemMenuHandle, MF_STRING, firstCustomID + i, "连接 " + server.ServerName);
                ++i;
            }
            this.disconnectID = firstCustomID + i;
            // 添加分隔符
            AppendMenu(systemMenuHandle, MF_SEPARATOR, 0, string.Empty);
            // 断开当前服务器
            AppendMenu(systemMenuHandle, MF_STRING, this.disconnectID, "断开当前服务器");
        }

        protected override void WndProc(ref Message m)
        {
            // 捕获系统菜单消息
            if (m.Msg == WM_SYSCOMMAND)
            {
                // 获取点击的菜单项 ID
                int menuItemID = m.WParam.ToInt32();
                // 处理自定义菜单项点击
                if (menuItemID == disconnectID)
                {
                    DisconnectHandle(currentShowIndex);
                }
                else if (menuItemID >= firstCustomID && menuItemID < disconnectID)
                {
                    ConnectHandle(menuItemID - firstCustomID);
                }
            }
            // 调用基类的 WndProc 方法以继续处理其他消息
            base.WndProc(ref m);
        }

        private void ConnectHandle(int index)
        {
            // 如果当前的服务器不是界面显示的服务器，则清空界面
            if (index != currentShowIndex)
            {
                this.Controls.Clear();
                // 修改分辨率
                this.MinimumSize = new System.Drawing.Size(640, 480);
                this.MaximumSize = new System.Drawing.Size(9999, 9999);
                this.Size = new System.Drawing.Size(640, 480);
                this.Text = title;
                currentShowIndex = -1;
            }

            // 载入当前rdpClient，填满全屏
            var rdpClient = this.JRdpObjList[index].RdpClient;
            ServerSetting serverSetting = serverSettings.ServerList[index];

            ((System.ComponentModel.ISupportInitialize)(rdpClient)).BeginInit();
            this.Controls.Add(rdpClient);
            currentShowIndex = index;
            rdpClient.Dock = DockStyle.Fill;
            ((System.ComponentModel.ISupportInitialize)(rdpClient)).EndInit();

            // 修改分辨率：非最大化使用配置值，最大化时使用当前分辨率
            if (this.WindowState != FormWindowState.Maximized)
            {
                // 非最大化时，计算差值，补偿整个窗口的大小，使得用户配置的大小等于RDP窗口大小
                if (deltaWidth < 0)
                {
                    deltaWidth = this.Size.Width - rdpClient.Size.Width;
                }
                if (deltaHeight < 0)
                {
                    deltaHeight = this.Size.Height - rdpClient.Size.Height;
                }
                // 让整个窗口分辨率=RDP显示分辨率+边框(deltaWidth, deltaHeight)
                this.Size = new System.Drawing.Size(serverSetting.Width + deltaWidth, serverSetting.Height + deltaHeight);
                // 窗体重新居中
                CenterToScreen();
            }
            // 锁定窗口分辨率
            this.MinimumSize = new System.Drawing.Size(this.Size.Width, this.Size.Height);
            this.MaximumSize = new System.Drawing.Size(this.Size.Width, this.Size.Height);

            // 如果当前的服务器已连接，不重复下发配置
            if (rdpClient.Connected == 1)
            {
                Trace.WriteLine("已连接");
                this.Text = this.JRdpObjList[index].ConnectMsg;
                return;
            }

            // RDP配置参数
            SetRdpParam(index, serverSetting);

            this.Text = title + "    " +
                serverSetting.ServerName + " " +
                serverSetting.Server + " " +
                serverSetting.UserName + " " +
                serverSetting.Port + " " +
                rdpClient.Size.Width + "x" + rdpClient.Size.Height + " " +
                DateTime.Now.ToString("HH:mm:ss") + " ";
            this.JRdpObjList[index].ConnectMsg = this.Text;
            Trace.WriteLine(this.Text + " " + index);

            // 连接服务器
            try
            {
                rdpClient.Connect();
            }
            catch (Exception ex)
            {
                HandleRdpException(ex);
            }
        }

        private void SetRdpParam(int index, ServerSetting serverSetting)
        {
            var rdpClient = this.JRdpObjList[index].RdpClient;
            rdpClient.Server = serverSetting.Server;
            rdpClient.UserName = serverSetting.UserName;
            rdpClient.DesktopWidth = rdpClient.Size.Width;
            rdpClient.DesktopHeight = rdpClient.Size.Height;
            rdpClient.ConnectingText = "";
            rdpClient.DisconnectedText = "";

            var settings = rdpClient.AdvancedSettings9;
            settings.singleConnectionTimeout = 3; // 设置连接超时时间（单位：毫秒）
            settings.overallConnectionTimeout = 10;
            settings.RedirectClipboard = true;
            settings.RedirectDrives = false;
            settings.RDPPort = serverSetting.Port;
            settings.ConnectToServerConsole = true;
            settings.ConnectToAdministerServer = true;
            settings.AuthenticationLevel = 0;
            settings.EnableCredSspSupport = true;
            settings.EnableAutoReconnect = true;
            settings.PerformanceFlags = 400; // 字体平滑、远程壁纸等设置
            settings.BandwidthDetection = true;
            // 密码
            IMsTscNonScriptable secured = (IMsTscNonScriptable)rdpClient.GetOcx();
            secured.ClearTextPassword = serverSetting.Password;
        }

        private void DisconnectHandle(int index)
        {
            if (index >= 0)
            {
                try
                {
                    var rdpClient = this.JRdpObjList[index].RdpClient;
                    if (rdpClient.Connected == 1)
                    {
                        rdpClient.Disconnect();
                    }
                    // 移除 RDP 客户端控件
                    this.Controls.Remove(rdpClient);
                    currentShowIndex = -1;
                    // rdpClient.Dispose(); // 释放资源
                }
                catch (Exception)
                {
                    // nothing to do
                }
            }
            // 修改分辨率
            this.MinimumSize = new System.Drawing.Size(640, 480);
            this.MaximumSize = new System.Drawing.Size(9999, 9999);
            this.Size = new System.Drawing.Size(640, 480);
            this.Text = title;
        }

        // 处理远程桌面连接异常
        private void HandleRdpException(Exception ex)
        {
            string errorMessage = "远程桌面连接失败：";

            if (ex is System.Runtime.InteropServices.COMException comEx)
            {
                // 处理 COM 异常
                switch (comEx.ErrorCode)
                {
                    case -2147467259: // 连接超时
                        errorMessage += "连接超时，请检查网络或服务器状态。";
                        break;
                    case -2147023174: // 网络不可达
                        errorMessage += "无法连接到服务器，请检查网络连接。";
                        break;
                    case -2147221164: // 认证失败
                        errorMessage += "用户名或密码错误，请检查凭据。";
                        break;
                    default:
                        errorMessage += $"未知错误：{comEx.Message}";
                        break;
                }
            }
            else
            {
                // 处理其他异常
                errorMessage += ex.Message;
            }

            // 显示错误信息
            this.Text = this.Text + " Error:" + errorMessage;
        }

        // 断开连接事件
        private void RdpClient_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            int code = e.discReason;
            string reason;
            // 查询https://learn.microsoft.com/zh-cn/windows/win32/termserv/imstscaxevents-ondisconnected
            switch (code)
            {
                case 0:
                    reason = "正常断开";
                    break;
                case 1:
                    reason = "网络连接中断";
                    break;
                case 2:
                    reason = "远程计算机已关闭连接";
                    break;
                case 3:
                    reason = "许可证问题";
                    break;
                case 4:
                    reason = "安全层协商失败";
                    break;
                case 5:
                    reason = "远程计算机已关闭连接";
                    break;
                default:
                    reason = $"未知，错误代码 {code}";
                    break;
            }
            // 显示断开连接信息
            this.Text = title + " Error:" + $"连接已断开，原因：{reason}";
            Trace.WriteLine($"连接已断开，原因：{reason}");
            ((AxMsRdpClient9NotSafeForScripting)sender).DisconnectedText = $"连接已断开，原因：{reason}";
        }
    }
}
