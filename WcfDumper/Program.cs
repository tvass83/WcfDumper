using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using WcfDumper.DataModel;
using WcfDumper.Helpers;

namespace WcfDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            CheckArgsAndPrintSyntax(args);

            var pids = ProcessHelper.GetPIDs(args[0]);
            Console.WriteLine($"Number of matching processes: {pids.Count}");

            for (int i = 0; i < pids.Count; i++)
            {
                Console.WriteLine($"Process {i+1} / {pids.Count}");

                int pid = pids[i];
                var wrapper = ClrMdHelper.AttachToLiveProcess(pid);

                wrapper.TypesToDump.Add(TYPE_ServiceDescription);

                wrapper.ClrInfoCallback = DumpClrInfo;

                wrapper.ClrHeapIsNotWalkableCallback = () =>
                {
                    Console.WriteLine("PID: {0} - Cannot walk the heap!", pid);
                };

                wrapper.ClrObjectOfTypeFoundCallback = DumpTypes;

                wrapper.Process();
            }

            // Display results
            foreach (var result in RESULTS)
            {
                Console.WriteLine($"PID: {result.ProcessInfo.PID}");
                Console.WriteLine("ServiceBehaviors: ");
                
                foreach (var svcBehavior in result.ServiceBehaviors)
                {
                    Console.WriteLine($"\t{svcBehavior}");
                }

                Console.WriteLine();
                Console.WriteLine("ServiceEndpoints:");

                foreach (var svcEndpoint in result.ServiceEndpoints)
                {
                    Console.WriteLine($"\t{result.ProcessInfo.PID} | {svcEndpoint.Contract} | {svcEndpoint.CallbackContract ?? "<n/a>"} | {svcEndpoint.Uri}");

                    if (svcEndpoint.EndpointBehaviors.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine("\tEndpointBehaviors:");

                        foreach (var endpointbehavior in svcEndpoint.EndpointBehaviors)
                        {
                            Console.WriteLine($"\t\t{endpointbehavior}");
                        }
                    }

                    if (svcEndpoint.ContractBehaviors.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine("\tContractBehaviors:");

                        foreach (var contractbehavior in svcEndpoint.ContractBehaviors)
                        {
                            Console.WriteLine($"\t\t{contractbehavior}");
                        }
                    }

                    if (svcEndpoint.ContractOperations.Any())
                    {
                        Console.WriteLine();
                        Console.WriteLine("\tOperations:");
                        
                        foreach (var operation in svcEndpoint.ContractOperations)
                        {
                            Console.WriteLine($"\t\t{operation.OperationName}");
                            Console.WriteLine("\t\t\tOperationBehaviors:");

                            foreach (var opBehavior in operation.OperationBehaviors)
                            {
                                Console.WriteLine($"\t\t\t\t{opBehavior}");
                            }
                        }
                    }
                }

                Console.WriteLine();
            }
        }

        private static void CheckArgsAndPrintSyntax(string[] args)
        {
            if (args == null || args.Length == 0 || args.Length > 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("WcfDumper <processname_with_wildcards>");
                Environment.Exit(1);
            }
        }

        private static void DumpTypes(ClrHeap heap, ulong obj, string type)
        {
            var resultItem = new ServiceDescriptionEntry((int)heap.Runtime.DataTarget.ProcessId);

            // Get ServiceEndpoint[]
            List<ulong> endpointObjs = ClrMdHelper.GetLastObjectInHierarchyAsArray(heap, obj, HIERARCHY_ServiceDescription_To_ServiceEndpoints, 0, TYPE_ServiceEndpointArray);

            foreach (var endpointObj in endpointObjs)
            {
                var epEntry = new ServiceEndpointEntry();
                                
                // Get Contract
                ulong contractObj = ClrMdHelper.GetLastObjectInHierarchy(heap, endpointObj, HIERARCHY_ServiceEndpoint_To_ContractType, 0);
                string contractTypeName = heap.GetObjectType(contractObj).GetRuntimeType(contractObj).Name;
                epEntry.Contract = contractTypeName;

                // Get IContractBehavior[]
                List<ulong> contractBehaviorObjs = ClrMdHelper.GetLastObjectInHierarchyAsArray(heap, endpointObj, HIERARCHY_ServiceEndpoint_To_ContractBehaviors, 0, TYPE_ContractBehaviorArray);

                foreach (var contractBehaviorObj in contractBehaviorObjs)
                {
                    ClrType itemType = heap.GetObjectType(contractBehaviorObj);
                    epEntry.ContractBehaviors.Add(itemType.Name);
                }

                // Get OperationDescription[]
                List<ulong> operationDescObjs = ClrMdHelper.GetLastObjectInHierarchyAsArray(heap, endpointObj, HIERARCHY_ServiceEndpoint_To_OperationDescriptions, 0, TYPE_OperationDescriptionArray);

                foreach (var operationDescObj in operationDescObjs)
                {
                    ulong opNameObj = ClrMdHelper.GetLastObjectInHierarchy(heap, operationDescObj, HIERARCHY_OperationDescription_To_Name, 0);
                    string opName = ClrMdHelper.GetObjectAs<string>(heap, opNameObj, FIELD_XmlName);
                    var odEntry = new OperationDescriptionEntry();
                    odEntry.OperationName = opName;

                    // Get IOperationBehavior[]
                    List<ulong> operationBehavObjs = ClrMdHelper.GetLastObjectInHierarchyAsArray(heap, operationDescObj, HIERARCHY_OperationDescription_To_OperationBehaviors, 0, TYPE_OperationBehaviorArray);

                    foreach (var operationBehavObj in operationBehavObjs)
                    {
                        ClrType itemType = heap.GetObjectType(operationBehavObj);
                        odEntry.OperationBehaviors.Add(itemType.Name);
                    }

                    epEntry.ContractOperations.Add(odEntry);
                }

                // Get CallbackContract
                ulong cbcontractObj = ClrMdHelper.GetLastObjectInHierarchy(heap, endpointObj, HIERARCHY_ServiceEndpoint_To_CallbackContractType, 0);
                
                if (cbcontractObj != 0)
                {
                    string cbcontractTypeName = heap.GetObjectType(cbcontractObj).GetRuntimeType(cbcontractObj).Name;
                    epEntry.CallbackContract = cbcontractTypeName;
                }

                // Get EndpointAddress URI
                ulong uriObj = ClrMdHelper.GetLastObjectInHierarchy(heap, endpointObj, HIERARCHY_ServiceEndpoint_To_Uri, 0);
                string uri = ClrMdHelper.GetObjectAs<string>(heap, uriObj, FIELD_UriName);
                epEntry.Uri = uri;

                // Get IEndpointBehavior[]
                List<ulong> endpBehaviorObjs = ClrMdHelper.GetLastObjectInHierarchyAsArray(heap, endpointObj, HIERARCHY_ServiceEndpoint_To_EndpointBehaviors, 0, TYPE_EndpointBehaviorArray);

                foreach (var endpBehaviorObj in endpBehaviorObjs)
                {
                    ClrType itemType = heap.GetObjectType(endpBehaviorObj);
                    epEntry.EndpointBehaviors.Add(itemType.Name);
                }
                
                resultItem.ServiceEndpoints.Add(epEntry);
            }

            // Get IServiceBehavior[]
            List<ulong> svcBehaviorObjs = ClrMdHelper.GetLastObjectInHierarchyAsArray(heap, obj, HIERARCHY_ServiceDescription_To_ServiceBehaviors, 0, TYPE_ServiceBehaviorArray);

            foreach (var svcBehaviorObj in svcBehaviorObjs)
            {
                ClrType svcBehaviorType = heap.GetObjectType(svcBehaviorObj);
                resultItem.ServiceBehaviors.Add(svcBehaviorType.Name);
            }

            RESULTS.Add(resultItem);
        }

        private static void DumpClrInfo(ClrInfo clrInfo)
        {
            Console.WriteLine("Found CLR Version: " + clrInfo.Version);

            // This is the data needed to request the dac from the symbol server:
            DacInfo dacInfo = clrInfo.DacInfo;
            Console.WriteLine("Dac File:  {0}", dacInfo.PlatformAgnosticFileName);

            // If we just happen to have the correct dac file installed on the machine,
            // the "LocalMatchingDac" property will return its location on disk:
            string dacLocation = clrInfo.LocalMatchingDac;

            if (!string.IsNullOrEmpty(dacLocation))
            {
                Console.WriteLine("Local dac location: " + dacLocation);
            }

            Console.WriteLine();
        }

        public static string[] HIERARCHY_ServiceDescription_To_ServiceEndpoints = new[] { "endpoints", "items", "_items" };
        public static string[] HIERARCHY_ServiceDescription_To_ServiceBehaviors = new[] { "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_Uri = new[] { "address", "uri" };
        public static string[] HIERARCHY_ServiceEndpoint_To_ContractType = new[] { "contract", "contractType" };
        public static string[] HIERARCHY_ServiceEndpoint_To_CallbackContractType = new[] { "contract", "callbackContractType" };
        public static string[] HIERARCHY_ServiceEndpoint_To_EndpointBehaviors = new[] { "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_ContractBehaviors = new[] { "contract", "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_OperationDescriptions = new[] { "contract", "operations", "items", "_items" };
        public static string[] HIERARCHY_OperationDescription_To_OperationBehaviors = new[] { "behaviors", "items", "_items" };
        public static string[] HIERARCHY_OperationDescription_To_Name = new[] { "name" };

        public const string FIELD_UriName = "m_String";
        public const string FIELD_XmlName = "decoded";

        public const string TYPE_ServiceDescription = "System.ServiceModel.Description.ServiceDescription";
        public const string TYPE_ServiceEndpointArray = "System.ServiceModel.Description.ServiceEndpoint[]";
        public const string TYPE_ServiceBehaviorArray = "System.ServiceModel.Description.IServiceBehavior[]";
        public const string TYPE_EndpointBehaviorArray = "System.ServiceModel.Description.IEndpointBehavior[]";
        public const string TYPE_ContractBehaviorArray = "System.ServiceModel.Description.IContractBehavior[]";
        public const string TYPE_OperationDescriptionArray = "System.ServiceModel.Description.OperationDescription[]";
        public const string TYPE_OperationBehaviorArray = "System.ServiceModel.Description.IOperationBehavior[]";

        private static List<ServiceDescriptionEntry> RESULTS = new List<ServiceDescriptionEntry>();
    }
}
