using Microsoft.Diagnostics.Runtime;

using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

using WcfDumper.DataModel;
using WcfDumper.Helpers;

namespace WcfDumper
{
    class Program
    {
        static void Main(string[] args)
        {
            var retCode = ArgParser.Parse(args, new string[0], new string[] { "-pids" });

            if (retCode != ErrorCode.Success)
            {
                PrintSyntaxAndExit(retCode);
            }

            List<ProcessInfo> procInfos;

            if (ArgParser.SwitchesWithValues.ContainsKey("-pids"))
            {
                var pids = ParseAndValidatePids();
                procInfos = pids.Select(x => ProcessHelper.GetProcessDetailsByPid(x)).ToList();
            }
            else
            {
                procInfos = ProcessHelper.GetProcessDetails(args[0]);
            }

            Console.WriteLine($"Number of matching processes: {procInfos.Count}");
            Console.WriteLine();

            if (procInfos.Any())
            {
                Console.WriteLine($"Data collection started.");
                Console.WriteLine();
            }

            for (int i = 0; i < procInfos.Count; i++)
            {
                int pid = procInfos[i].PID;                                

                Console.WriteLine($"Process {i + 1}/{procInfos.Count}");

                if (string.IsNullOrWhiteSpace(procInfos[i].Name))
                {
                    Console.WriteLine($"WARNING: Process with pid '{pid}' does not exist.");
                    Console.WriteLine();
                    continue;
                }

                Console.WriteLine($"Process: {procInfos[i].Name} ({pid})");
                Console.WriteLine($"CmdLine: {procInfos[i].CmdLine}");
                var wrapper = ClrMdHelper.AttachToLiveProcess(pid);

                wrapper.TypesToDump.Add(TYPE_ServiceDescription);

                wrapper.ClrHeapIsNotWalkableCallback = () =>
                {
                    Console.WriteLine("PID: {0} - Cannot walk the heap!", pid);
                };

                wrapper.ClrObjectOfTypeFoundCallback = DumpTypes;

                wrapper.Process();

                Console.WriteLine();
            }

            if (procInfos.Any())
            {
                Console.WriteLine($"Data collection completed.");
                Console.WriteLine();
            }

            // Display results
            foreach (var group in RESULTS.GroupBy(x => x.Pid))
            {
                var proc = procInfos.First(x => x.PID == group.Key);
                Console.WriteLine($"Displaying data for:");
                Console.WriteLine($"\tProcess: {proc.Name} ({proc.PID})");
                Console.WriteLine($"\tCmdLine: {proc.CmdLine}");
                Console.WriteLine();
                
                int cnt = 0;
                int cntAll = group.Count();

                foreach (var result in group)
                {
                    Console.WriteLine($"ServiceDescription {++cnt}/{cntAll}");
                    Console.WriteLine($"----------------------");
                    Console.WriteLine();
                    Console.WriteLine("ServiceBehaviors: ");

                    foreach (var svcBehavior in result.ServiceBehaviors)
                    {
                        Console.WriteLine($"\t{svcBehavior}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("ServiceEndpoints:");

                    foreach (var svcEndpoint in result.ServiceEndpoints)
                    {
                        Console.WriteLine($"\t{proc.PID} | {svcEndpoint.Contract} | {svcEndpoint.CallbackContract ?? "<n/a>"} | {svcEndpoint.Uri}");
                        Console.WriteLine($"\t{svcEndpoint.BindingSecurity}");

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
        }

        private static List<int> ParseAndValidatePids()
        {
            var pids = ArgParser.SwitchesWithValues["-pids"].Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            var ret = new List<int>();

            foreach (var pid in pids)
            {
                if (int.TryParse(pid, out int result))
                {
                    ret.Add(result);
                }
                else
                {
                    Console.WriteLine($"ERROR: invalid process id: '{pid}'");
                    Environment.Exit(1);
                }
            }

            return ret;
        }

        private static void PrintSyntaxAndExit(ErrorCode errorCode)
        {
            Console.WriteLine($"Syntax error ({errorCode})");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("WcfDumper <processname_with_wildcards>");
            Console.WriteLine("OR");
            Console.WriteLine("WcfDumper -pids pid1[;pid2;...;pidn]");
            Environment.Exit(1);
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

                if (contractObj == 0)
                {
                    continue;
                }

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

                // Read binding security information.
                ulong bindingObj = ClrMdHelper.GetLastObjectInHierarchy(heap, endpointObj, new [] {"binding"}, 0);
                ClrType bindingType = heap.GetObjectType(bindingObj);

                if (bindingType == null)
                {
                    epEntry.BindingSecurity = new UnknownBindingSecurity();
                }
                else if (bindingType.Name.EndsWith("NetTcpBinding"))
                {
                    ulong securityObj = ClrMdHelper.GetLastObjectInHierarchy(heap, bindingObj, new[] { "security" }, 0);

                    NetTcpBindingSecurity netTcpBindingSecurity = new NetTcpBindingSecurity();
                    netTcpBindingSecurity.SecurityMode = ClrMdHelper.GetObjectAs<SecurityMode>(heap, securityObj, FIELD_NetTcpBindingSecurityMode);

                    ulong tcpTransportSecurityObj = ClrMdHelper.GetLastObjectInHierarchy(heap, securityObj, HIERARCHY_NetTcpBindingSecurity_To_TcpTransportSecurity, 0);
                    netTcpBindingSecurity.ClientCredentialType = (TcpClientCredentialType)ClrMdHelper.GetObjectAs<int>(heap, tcpTransportSecurityObj, FIELD_NetTcpBindingClientCredentialType);

                    epEntry.BindingSecurity = netTcpBindingSecurity;
                }
                else if (bindingType.Name.EndsWith("NetNamedPipeBinding"))
                {
                    ulong securityObj = ClrMdHelper.GetLastObjectInHierarchy(heap, bindingObj, new[] { "security" }, 0);

                    NetNamedPipeBindingSecurity netNamedPipeSecurity = new NetNamedPipeBindingSecurity();
                    netNamedPipeSecurity.SecurityMode = ClrMdHelper.GetObjectAs<NetNamedPipeSecurityMode>(heap, securityObj, FIELD_NetTcpBindingSecurityMode);

                    epEntry.BindingSecurity = netNamedPipeSecurity;
                }
                else
                {
                    epEntry.BindingSecurity = new UnknownBindingSecurity(bindingType.Name);
                }

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

        public static string[] HIERARCHY_OperationDescription_To_Name = new[] { "name" };
        public static string[] HIERARCHY_OperationDescription_To_OperationBehaviors = new[] { "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceDescription_To_ServiceBehaviors = new[] { "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceDescription_To_ServiceEndpoints = new[] { "endpoints", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_CallbackContractType = new[] { "contract", "callbackContractType" };
        public static string[] HIERARCHY_ServiceEndpoint_To_ContractBehaviors = new[] { "contract", "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_ContractType = new[] { "contract", "contractType" };
        public static string[] HIERARCHY_ServiceEndpoint_To_EndpointBehaviors = new[] { "behaviors", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_OperationDescriptions = new[] { "contract", "operations", "items", "_items" };
        public static string[] HIERARCHY_ServiceEndpoint_To_Uri = new[] { "address", "uri" };
        public static string[] HIERARCHY_NetTcpBindingSecurity_To_TcpTransportSecurity = new[] { "transportSecurity"};

        public const string FIELD_UriName = "m_String";
        public const string FIELD_XmlName = "decoded";
        public const string FIELD_NetTcpBindingSecurityMode = "mode";
        public const string FIELD_NetTcpBindingClientCredentialType = "clientCredentialType";

        public const string TYPE_ContractBehaviorArray = "System.ServiceModel.Description.IContractBehavior[]";
        public const string TYPE_EndpointBehaviorArray = "System.ServiceModel.Description.IEndpointBehavior[]";
        public const string TYPE_ServiceBehaviorArray = "System.ServiceModel.Description.IServiceBehavior[]";
        public const string TYPE_ServiceDescription = "System.ServiceModel.Description.ServiceDescription";
        public const string TYPE_ServiceEndpointArray = "System.ServiceModel.Description.ServiceEndpoint[]";
        public const string TYPE_OperationBehaviorArray = "System.ServiceModel.Description.IOperationBehavior[]";
        public const string TYPE_OperationDescriptionArray = "System.ServiceModel.Description.OperationDescription[]";

        private static readonly List<ServiceDescriptionEntry> RESULTS = new List<ServiceDescriptionEntry>();
    }
}
