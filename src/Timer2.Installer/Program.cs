using System;
using System.IO;
using System.Windows.Forms;

namespace Timer2.Installer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                    LogCrash("AppDomain: " + e.ExceptionObject?.ToString());
                };
                Application.ThreadException += (s, e) => {
                    LogCrash("ThreadException: " + e.Exception?.ToString());
                };

                ApplicationConfiguration.Initialize();
                Application.Run(new InstallerWizardForm());
            }
            catch (Exception ex)
            {
                string desktopLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "setup_error.txt");
                File.WriteAllText(desktopLog, ex.ToString());
                MessageBox.Show("Setup Wizard Startup Error:\r\n" + ex.ToString(), "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LogCrash(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installer_crash.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
            }
            catch { }
        }
    }
}
