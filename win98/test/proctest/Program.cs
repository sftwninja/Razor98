// checks the Process bits Razor uses actually work on 98 / .NET 2.0.
// writes C:\TEST\PROC.LOG.
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

static class Program
{
    [StructLayout(LayoutKind.Sequential)]
    struct STARTUPINFO
    {
        public int cb;
        public IntPtr lpReserved, lpDesktop, lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
        public short wShowWindow, cbReserved2;
        public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PROCESS_INFORMATION
    {
        public IntPtr hProcess, hThread;
        public int dwProcessId, dwThreadId;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    static extern bool CreateProcess(string app, string cmd, IntPtr pa, IntPtr ta, bool inherit, int flags,
        IntPtr env, string dir, ref STARTUPINFO si, out PROCESS_INFORMATION pi);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetExitCodeProcess(IntPtr process, out uint exitCode);

    [DllImport("kernel32.dll")]
    static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    static extern IntPtr LoadLibrary(string path);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
    static extern IntPtr GetProcAddress(IntPtr module, string name);

    static readonly string[] CryptExports = {
        "CalibratePosition", "FindUOWindow", "GetCommMutex", "GetPacketLength", "GetSharedAddress",
        "GetUOProcId", "GetUOVersion", "InstallLibrary", "IsDynLength", "OnAttach", "SetDataPath",
        "SetServer", "Shutdown", "TotalIn", "TotalOut", "WaitForWindow" };

    // GetProcAddress every export like DllImport does, 9x is picky about names
    static void CheckExports(string path)
    {
        IntPtr h = LoadLibrary(path);
        if (h == IntPtr.Zero)
        {
            Log("LoadLibrary " + path + " failed " + Marshal.GetLastWin32Error());
            return;
        }
        string missing = "";
        foreach (string name in CryptExports)
            if (GetProcAddress(h, name) == IntPtr.Zero)
                missing += " " + name;
        Log("exports " + path + ": " + (missing.Length == 0 ? "all found" : "MISSING" + missing));
    }

    [DllImport("C:\\RAZOR\\Loader.dll")]
    static extern uint Load(string exe, string dll, string func, IntPtr dllData, int dataLen, out uint pid);

    static StreamWriter log;

    static void Log(string s)
    {
        log.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + s);
        log.Flush();
    }

    delegate object Probe();

    static void Try(string what, Probe probe)
    {
        try
        {
            object v = probe();
            Log(what + " = " + (v == null ? "null" : v.ToString()));
        }
        catch (Exception e)
        {
            Log(what + " THREW " + e.GetType().Name + ": " + e.Message);
        }
    }

    static void Main(string[] args)
    {
        Directory.CreateDirectory(@"C:\TEST");
        log = new StreamWriter(@"C:\TEST\PROC.LOG", true);
        try
        {
            Log("OS " + Environment.OSVersion + ", CLR " + Environment.Version);
            CheckExports(@"C:\RAZOR\Crypt.dll");
            if (args.Length > 0 && args[0] == "load")
            {
                // same as OSIClient.LaunchClient
                uint lpid;
                uint rc = Load(@"C:\UO\client.exe", @"C:\RAZOR\Crypt.dll", "OnAttach", IntPtr.Zero, 0, out lpid);
                Log("Loader Load() = " + rc + ", pid 0x" + lpid.ToString("X8") + ", lastError " + Marshal.GetLastWin32Error());
                Thread.Sleep(5000);
                Try("GetProcessById(loaded)", delegate { return Process.GetProcessById((int)lpid).ProcessName; });
                Log("done");
                return;
            }
            string exe = args.Length > 0 ? args[0] : @"C:\WINDOWS\NOTEPAD.EXE";
            STARTUPINFO si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(typeof(STARTUPINFO));
            PROCESS_INFORMATION pi;
            if (!CreateProcess(exe, null, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref si, out pi))
            {
                Log("CreateProcess failed " + Marshal.GetLastWin32Error());
                return;
            }
            int pid = pi.dwProcessId;
            Log("CreateProcess pid=" + pid + " (0x" + pid.ToString("X8") + ")");
            Thread.Sleep(2000);

            Process p = null;
            Try("GetProcessById", delegate { p = Process.GetProcessById(pid); return p; });
            if (p != null)
            {
                Try("p.Id", delegate { return p.Id; });
                Try("p.HasExited", delegate { return p.HasExited; });
                Try("p.MainWindowHandle", delegate { return p.MainWindowHandle; });
                Try("p.PriorityClass", delegate { return p.PriorityClass; });
                Try("p.ProcessName", delegate { return p.ProcessName; });
            }
            Try("OpenProcess+GetExitCodeProcess", delegate
            {
                IntPtr h = OpenProcess(0x0400, false, pid);
                if (h == IntPtr.Zero) return "OpenProcess failed " + Marshal.GetLastWin32Error();
                uint code;
                bool ok = GetExitCodeProcess(h, out code);
                CloseHandle(h);
                return ok ? "exitCode 0x" + code.ToString("X") : "GetExitCodeProcess failed";
            });
            if (p != null)
                Try("p.CloseMainWindow", delegate { return p.CloseMainWindow(); });
            Thread.Sleep(2000);
            if (p != null)
                Try("p.HasExited after close", delegate { return p.HasExited; });
            Try("GetCurrentProcess().Id", delegate { return Process.GetCurrentProcess().Id; });
            Log("done");
        }
        catch (Exception e)
        {
            Log("EXC " + e);
        }
        log.Close();
    }
}
