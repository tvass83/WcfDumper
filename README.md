**WcfDumper** is a command-line tool (based on "[CLR MD](https://github.com/microsoft/clrmd)") that dumps WCF ServiceDescription objects from live processes.
The tool can be used to get a system-wide overview of the WCF endpoints exposed.

### WCF objects dumped and the relationship among them
![WCF objects dumped](./docs/WCF_objects.svg)

### Usage
Just pass a process name (or regex) as an argument and you'll get a similar output:

```
>WcfDumper.exe wcfte.*
Number of matching processes: 1

Data collection started.

Process 1 / 1
Process: WcfTest.exe (2376)
CmdLine: "D:\WcfTest.exe"

Data collection completed.

Displaying data for:
        Process: WcfTest.exe (2376)
        CmdLine: "D:\WcfTest.exe"

ServiceDescription 1/1
----------------------

ServiceBehaviors:
        System.ServiceModel.ServiceBehaviorAttribute
        System.ServiceModel.Description.ServiceAuthenticationBehavior
        System.ServiceModel.Description.ServiceAuthorizationBehavior
        System.ServiceModel.Description.ServiceDebugBehavior
        WcfTest.Behaviors.DummyServiceBehavior

ServiceEndpoints:
        2376 | WcfTest.IPingService | WcfTest.IPingServiceCallback | net.tcp://localhost:9999/PingService

        EndpointBehaviors:
                WcfTest.Behaviors.DummyEndpointBehavior

        ContractBehaviors:
                System.ServiceModel.Dispatcher.OperationSelectorBehavior
                WcfTest.Behaviors.DummyContractBehavior

        Operations:
                Ping
                        OperationBehaviors:
                                System.ServiceModel.Dispatcher.OperationInvokerBehavior
                                System.ServiceModel.OperationBehaviorAttribute
                                System.ServiceModel.Description.DataContractSerializerOperationBehavior
                                System.ServiceModel.Description.DataContractSerializerOperationGenerator
                                WcfTest.Behaviors.DummyOperationBehavior
                Reply
                        OperationBehaviors:
                                System.ServiceModel.Dispatcher.OperationInvokerBehavior
                                System.ServiceModel.OperationBehaviorAttribute
                                System.ServiceModel.Description.DataContractSerializerOperationBehavior
                                System.ServiceModel.Description.DataContractSerializerOperationGenerator
                                WcfTest.Behaviors.DummyOperationBehavior
```
If you're only interested in the exposed WCF endpoint URIs then pipe the output to the find command like this:
```
>WcfDumper.exe wcfte.* | find "://"
5132 | WcfTest.IPingService | WcfTest.IPingServiceCallback | net.tcp://localhost:9999/PingService
5132 | WcfTest.IPingService | WcfTest.IPingServiceCallback | net.pipe://localhost/PingService
```
This way you can quickly identify the host process of a particular WCF service.
