using System.Collections.Generic;

namespace WcfDumper.DataModel
{
    public class ServiceDescriptionEntry
    {
        public ServiceDescriptionEntry(int pid)
        {
            ProcessInfo = new ProcessInfo();
            ProcessInfo.PID = pid;
        }

        public ProcessInfo ProcessInfo;
        public List<string> ServiceBehaviors = new List<string>();
        public List<ServiceEndpointEntry> ServiceEndpoints = new List<ServiceEndpointEntry>();
    }
}
