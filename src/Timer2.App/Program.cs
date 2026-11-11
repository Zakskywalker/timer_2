using System;
using System.Windows.Forms;
using Timer2.App.Forms;
using Timer2.App.Updates;

namespace Timer2.App
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--apply-update")
            {
                Environment.ExitCode = AppUpdateService.ApplyDownloadedUpdate(args);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new SplashScreenForm(args));
        }
    }
}
