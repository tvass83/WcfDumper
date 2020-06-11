using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WcfDumper.Helpers
{
    internal static class NativeMethods
    {
        public static bool Is64Bit(int pid)
        {
            if (!Environment.Is64BitOperatingSystem)
            {
                return false;
            }

            bool isWow64;

            using (var process = Process.GetProcessById(pid))
            {
                if (!IsWow64Process(process.Handle, out isWow64))
                {
                    throw new Win32Exception();
                }
            }

            return !isWow64;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
    }
}
