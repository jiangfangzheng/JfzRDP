using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace JfzRdp
{
    public sealed class AboutForm : Form
    {
        private const string HomepageUrl = "https://jfz.me";
        private const string GithubUrl = "https://github.com/jiangfangzheng/JfzRDP";

        public AboutForm()
        {
            Text = "About JfzRDP";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.None;
            Font = SystemFonts.MessageBoxFont;
            ClientSize = new Size(300, 170);

            var titleLabel = new Label
            {
                Text = "异想家RDP远程桌面管理 v2.0",
                AutoSize = true,
                Location = new Point(20, 20)
            };
            Controls.Add(titleLabel);

            var authorLabel = new Label
            {
                Text = "by 异想家Sandeepin",
                AutoSize = true,
                Location = new Point(20, 46)
            };
            Controls.Add(authorLabel);

            var linksPanel = new FlowLayoutPanel
            {
                Location = new Point(16, 76),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = Padding.Empty
            };

            var homepageLink = CreateLinkLabel("作者主页", HomepageUrl);
            homepageLink.Margin = new Padding(0, 0, 18, 0);
            linksPanel.Controls.Add(homepageLink);

            var githubLink = CreateLinkLabel("Github", GithubUrl);
            githubLink.Margin = new Padding(0);
            linksPanel.Controls.Add(githubLink);

            Controls.Add(linksPanel);

            var okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 30),
                Location = new Point(ClientSize.Width - 110, ClientSize.Height - 45)
            };
            Controls.Add(okButton);
            AcceptButton = okButton;
            CancelButton = okButton;
        }

        private LinkLabel CreateLinkLabel(string text, string url)
        {
            var link = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                LinkBehavior = LinkBehavior.SystemDefault
            };
            link.LinkClicked += delegate { OpenUrl(url); };
            return link;
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    "无法打开链接：\r\n" + ex.Message,
                    "About JfzRDP",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
