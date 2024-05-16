using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskManager.Shared
{
    internal static class Windows
    {
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        internal static string GetWindowsHeader(IntPtr wind)
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            if (GetWindowText(wind, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }
            return null;
        }
    }
}
