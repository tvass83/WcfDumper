using System.Collections.Generic;

namespace WcfDumper.DataModel
{
    public class ServiceEndpointEntry
    {
        public string Uri;
        public string Contract;
        public string CallbackContract;
        public List<string> EndpointBehaviors = new List<string>();
        public List<string> ContractBehaviors = new List<string>();
        public List<OperationDescriptionEntry> ContractOperations = new List<OperationDescriptionEntry>();
    }
}
