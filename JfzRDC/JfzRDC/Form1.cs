using AxMSTSCLib;
using MSTSCLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace JfzRDC
{
    public partial class Form1 : Form
    {

        // Windows API 函数
        private const int WM_SYSCOMMAND = 0x0112;
        private const int MF_STRING = 0x0000;
        private const int MF_SEPARATOR = 0x0800;

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string lpNewItem);

        // 自定义菜单项的 ID
        private const int CustomMenuItem1ID = 1000;
        private const int CustomMenuItem2ID = 1001;


        public Form1()
        {
            InitializeComponent();
            // 设置连接超时时间（单位：毫秒）
            // this.axMsTscAxNotSafeForScripting1.AdvancedSettings7.ConnectionTimeout = 10000; // 10秒
            // 绑定事件
            this.axMsTscAxNotSafeForScripting1.OnDisconnected += RdpClient_OnDisconnected;
            // 订阅 FormClosing 事件
            this.FormClosing += MainForm_FormClosing;
            // 添加自定义菜单项
            AddCustomMenuItems();
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
            // 添加自定义菜单项
            AppendMenu(systemMenuHandle, MF_STRING, CustomMenuItem1ID, "连接");
            AppendMenu(systemMenuHandle, MF_STRING, CustomMenuItem2ID, "断开");

        }

        protected override void WndProc(ref Message m)
        {
            // 捕获系统菜单消息
            if (m.Msg == WM_SYSCOMMAND)
            {
                // 获取点击的菜单项 ID
                int menuItemID = m.WParam.ToInt32();

                // 处理自定义菜单项点击
                switch (menuItemID)
                {
                    case CustomMenuItem1ID:
                        ConnectHandle();
                        break;
                    case CustomMenuItem2ID:
                        DisconnectHandle();
                        break;
                }
            }

            // 调用基类的 WndProc 方法以继续处理其他消息
            base.WndProc(ref m);
        }

        private void ConnectHandle()
        {
            if (axMsTscAxNotSafeForScripting1.Connected == 1)
            {
                return;
            }
            // 获取服务器信息Json内容
            string filePath = Path.Combine(AppContext.BaseDirectory, "server.json");
            string jsonContent = File.ReadAllText(filePath);
            ServerSettings serverSettings = JsonSerializer.Deserialize<ServerSettings>(jsonContent);
            // 修改分辨率：非最大化使用配置值，最大化时使用当前分辨率
            if (this.WindowState != FormWindowState.Maximized)
            {
                // 非最大化时，计算差值，补偿整个窗口的大小，使得用户配置的大小等于RDP窗口大小
                int deltaWidth = this.Size.Width - axMsTscAxNotSafeForScripting1.Size.Width;
                int deltaHeight = this.Size.Height - axMsTscAxNotSafeForScripting1.Size.Height;
                this.Size = new System.Drawing.Size(serverSettings.Width + deltaWidth, serverSettings.Height + deltaHeight);
                CenterToScreen(); // 窗体重新居中
            }
            this.MinimumSize = new System.Drawing.Size(this.Size.Width, this.Size.Height);
            this.MaximumSize = new System.Drawing.Size(this.Size.Width, this.Size.Height);

            axMsTscAxNotSafeForScripting1.Server = serverSettings.Server;
            axMsTscAxNotSafeForScripting1.UserName = serverSettings.UserName;
            axMsTscAxNotSafeForScripting1.DesktopWidth = axMsTscAxNotSafeForScripting1.Size.Width;
            axMsTscAxNotSafeForScripting1.DesktopHeight = axMsTscAxNotSafeForScripting1.Size.Height;
            axMsTscAxNotSafeForScripting1.ConnectingText = "";
            axMsTscAxNotSafeForScripting1.DisconnectedText = "";

            IMsRdpClientAdvancedSettings7 AdvancedSettings7 = (IMsRdpClientAdvancedSettings7)axMsTscAxNotSafeForScripting1.AdvancedSettings;
            AdvancedSettings7.RedirectClipboard = true;
            AdvancedSettings7.RedirectDrives = false;
            AdvancedSettings7.RDPPort = serverSettings.Port;
            AdvancedSettings7.ConnectToServerConsole = true;
            AdvancedSettings7.ConnectToAdministerServer = true;
            AdvancedSettings7.AuthenticationLevel = 0;
            AdvancedSettings7.EnableCredSspSupport = true;
            // 字体平滑、远程壁纸等设置
            AdvancedSettings7.PerformanceFlags = 384;

            IMsTscNonScriptable secured = (IMsTscNonScriptable)axMsTscAxNotSafeForScripting1.GetOcx();
            secured.ClearTextPassword = serverSettings.Password;

            this.MaximumSize = new Size(this.Size.Width, this.Size.Height);

            this.Text = "异想家RDP远程桌面管理 v1.0     " + serverSettings.Server + " " + serverSettings.UserName +
                " " + serverSettings.Port + " " + axMsTscAxNotSafeForScripting1.Size.Width +
                "x" + axMsTscAxNotSafeForScripting1.Size.Height + " " + DateTime.Now.ToString("HH:mm:ss") + " ";

            try
            {
                axMsTscAxNotSafeForScripting1.Connect();
            }
            catch (Exception ex)
            {
                // 捕获并处理异常
                HandleRdpException(ex);
            }
        }

        private void DisconnectHandle()
        {
            try
            {
                if (axMsTscAxNotSafeForScripting1.Connected == 1)
                {
                    axMsTscAxNotSafeForScripting1.Disconnect();
                }
            }
            catch (Exception)
            {

            }
            // 修改分辨率
            //axMsTscAxNotSafeForScripting1.Size = new System.Drawing.Size(624, 411);
            this.MinimumSize = new System.Drawing.Size(640, 480);
            this.MaximumSize = new System.Drawing.Size(9999, 9999);
            this.Size = new System.Drawing.Size(640, 480);
            this.Text = "异想家RDP远程桌面管理 v1.0";
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
            // axMsTscAxNotSafeForScripting1.ConnectingText = errorMessage;
            // axMsTscAxNotSafeForScripting1.DisconnectedText = errorMessage;
            // MessageBox.Show(errorMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // 断开连接事件
        private void RdpClient_OnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            // 显示断开连接信息
            axMsTscAxNotSafeForScripting1.DisconnectedText = $"连接已断开，原因代码：{e.discReason}";
            // MessageBox.Show($"连接已断开，原因代码：{e.discReason}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
