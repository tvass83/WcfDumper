using System.ServiceModel;

namespace WcfDumper.DataModel
{
    /// <summary>
    /// Binding security for named pipe bindings.
    /// </summary>
    public class NetNamedPipeBindingSecurity : IBindingSecurity
    {
        public NetNamedPipeSecurityMode SecurityMode;

        public override string ToString()
        {
            return $"Security Mode: {SecurityMode}";
        }
    }
}
