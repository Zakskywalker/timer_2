using System;
using System.Windows.Forms;
using Timer2.App.Forms;

namespace Timer2.App
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new SplashScreenForm());
        }
    }
}
