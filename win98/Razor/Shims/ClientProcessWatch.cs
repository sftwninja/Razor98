// Process.HasExited opens a handle every call, which is slow on 98 and
// Razor checks this several times a tick. keep one open instead.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Assistant
{
    internal static class ClientProcessWatch
    {
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint STILL_ACTIVE = 259;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        private static Process s_Process;
        private static IntPtr s_Handle = IntPtr.Zero;

        public static bool IsRunning(Process process)
        {
            if (process == null)
                return false;

            if (!ReferenceEquals(process, s_Process))
            {
                if (s_Handle != IntPtr.Zero)
                    CloseHandle(s_Handle);
                s_Process = process;
                s_Handle = OpenProcess(PROCESS_QUERY_INFORMATION, false, process.Id);
            }

            uint code;
            if (s_Handle == IntPtr.Zero || !GetExitCodeProcess(s_Handle, out code))
                return !process.HasExited; // fall back to upstream's check

            return code == STILL_ACTIVE;
        }
    }
}
