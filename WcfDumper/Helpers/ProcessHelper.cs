using System.Collections.Generic;
using System.Linq;
using System.Management;
using WcfDumper.DataModel;

namespace WcfDumper.Helpers
{
    public static class ProcessHelper
    {        
        public static List<ProcessInfo> GetProcessDetails(string processNameWildCards)
        {
            return GetProcessDetailsImpl($"select {CONST_ProcessId},{CONST_ProcessName},{CONST_CommandLine} from Win32_Process where {CONST_ProcessName} like '{processNameWildCards.Replace("*", "%")}'");
        }

        public static ProcessInfo GetProcessDetailsByPid(int pid)
        {
            var pInfo = GetProcessDetailsImpl($"select {CONST_ProcessId},{CONST_ProcessName},{CONST_CommandLine} from Win32_Process where {CONST_ProcessId}={pid}")
                .SingleOrDefault();

            return pInfo ?? new ProcessInfo() { PID = pid };
        }

        private static List<ProcessInfo> GetProcessDetailsImpl(string query)
        {
            var ret = new List<ProcessInfo>();

            using (var mos = new ManagementObjectSearcher(query))
            {
                foreach (ManagementBaseObject item in mos.Get())
                {
                    var pi = new ProcessInfo
                    {
                        PID = (int)(uint)item[CONST_ProcessId],
                        Name = (string)item[CONST_ProcessName],
                        CmdLine = (string)item[CONST_CommandLine]
                    };

                    ret.Add(pi);
                }
            }

            return ret.OrderBy(x => x.Name).ToList();
        }

        private const string CONST_ProcessId = "ProcessId";
        private const string CONST_ProcessName = "Name";
        private const string CONST_CommandLine = "CommandLine";
    }
}
