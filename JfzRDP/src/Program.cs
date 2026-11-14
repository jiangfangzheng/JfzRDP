using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using JfzRdp.Models;
using JfzRdp.Services;

namespace JfzRdp
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var configPath = Path.Combine(Application.StartupPath, "server.json");
            bool createdDefault;
            string warning;
            var config = ConfigStore.LoadOrCreate(configPath, out createdDefault, out warning);

            if (!string.IsNullOrEmpty(warning))
            {
                MessageBox.Show(
                    warning,
                    "server.json",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            Application.Run(new MainForm(config));
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show(
                "程序发生未处理的错误：\r\n\r\n" + e.Exception.Message,
                "JfzRDP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            MessageBox.Show(
                "程序发生未处理的错误：\r\n\r\n" + (exception == null ? "未知错误" : exception.Message),
                "JfzRDP",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
