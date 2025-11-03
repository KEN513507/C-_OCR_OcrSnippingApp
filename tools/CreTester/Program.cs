using System;
using System.Threading;
using System.Windows.Forms;

namespace CreTester
{
    internal static class Program
    {
        private const string MutexName = @"Global\CreTester_SingleInstance";

        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(false, MutexName);
            if (!mutex.WaitOne(0))
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
