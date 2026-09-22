using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinSelectionColor
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            try
            {
                // Enable modern per-monitor DPI scaling if supported
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    SetProcessDPIAware();
                }
            }
            catch { }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла непредвиденная ошибка:\n" + ex.Message,
                    "Ошибка WinSelectionColor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
