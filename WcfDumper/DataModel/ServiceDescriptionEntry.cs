using System.Collections.Generic;

namespace WcfDumper.DataModel
{
    public class ServiceDescriptionEntry
    {
        public ServiceDescriptionEntry(int pid)
        {
            Pid = pid;
        }

        public int Pid;
        public List<string> ServiceBehaviors = new List<string>();
        public List<ServiceEndpointEntry> ServiceEndpoints = new List<ServiceEndpointEntry>();
    }
}
