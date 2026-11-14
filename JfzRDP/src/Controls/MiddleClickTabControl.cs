using System;
using System.Drawing;
using System.Windows.Forms;

namespace JfzRdp.Controls
{
    /// <summary>
    /// TabControl 的鼠标中键点击在原生控件里不会可靠地引发 MouseDown，
    /// 这里直接处理窗口消息，保证中键可以关闭 RDP 标签页。
    /// </summary>
    public sealed class MiddleClickTabControl : TabControl
    {
        private const int WmMButtonDown = 0x0207;
        private const int WmMButtonUp = 0x0208;

        public event Action<TabPage> TabMiddleClicked;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmMButtonDown)
            {
                var index = HitTestTab(Cursor.Position);
                if (index >= 0)
                {
                    var page = TabPages[index];
                    BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed)
                        {
                            return;
                        }

                        var handler = TabMiddleClicked;
                        if (handler != null && TabPages.Contains(page))
                        {
                            handler(page);
                        }
                    }));
                    return;
                }
            }
            else if (m.Msg == WmMButtonUp)
            {
                if (HitTestTab(Cursor.Position) >= 0)
                {
                    return;
                }
            }

            base.WndProc(ref m);
        }

        private int HitTestTab(Point screenPoint)
        {
            var clientPoint = PointToClient(screenPoint);
            for (var i = 0; i < TabCount; i++)
            {
                if (GetTabRect(i).Contains(clientPoint))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
