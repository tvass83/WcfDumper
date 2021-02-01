using System.ServiceModel;


namespace WcfDumper.DataModel
{
    /// <summary>
    /// Binding security for net tcp bindings.
    /// </summary>
    public class NetTcpBindingSecurity : IBindingSecurity
    {
        public SecurityMode SecurityMode;
        public TcpClientCredentialType ClientCredentialType;

        public override string ToString()
        {
            return $"Security Mode: '{SecurityMode}' Client Credential Type: '{ClientCredentialType}'";
        }
    }
}