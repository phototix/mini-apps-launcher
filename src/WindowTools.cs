using System;
using System.Runtime.InteropServices;

namespace MiniAppsLauncher
{
    internal static class WindowTools
    {
        private const int SwRestore = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        public static void BringToFront(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                return;
            }

            try
            {
                if (IsIconic(handle))
                {
                    ShowWindow(handle, SwRestore);
                }

                uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
                uint currentThread = GetCurrentThreadId();
                bool attached = false;

                if (foregroundThread != 0 && foregroundThread != currentThread)
                {
                    attached = AttachThreadInput(foregroundThread, currentThread, true);
                }

                BringWindowToTop(handle);
                SetForegroundWindow(handle);

                if (attached)
                {
                    AttachThreadInput(foregroundThread, currentThread, false);
                }
            }
            catch (Exception ex)
            {
                Log.Error("BringToFront failed", ex);
            }
        }
    }
}
