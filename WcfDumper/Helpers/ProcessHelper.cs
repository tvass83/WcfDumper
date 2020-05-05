using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WcfDumper.DataModel;

namespace WcfDumper.Helpers
{
    public static class ProcessHelper
    {
        public static ProcessInfo GetProcessDetails(int pid)
        {
            var pi = new ProcessInfo();
            pi.PID = pid;

            return pi;
        }

        public static List<int> GetPIDs(string wildcard)
        {            
            var processes = Process.GetProcesses();
            var ret = new List<int>();
            Regex regex = new Regex(wildcard, RegexOptions.IgnoreCase);
            
            foreach (var process in processes)
            {
                using (process)
                {
                    if (regex.IsMatch(process.ProcessName))
                    {
                        ret.Add(process.Id);
                    }
                }
            }

            return ret;
        }

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
}
