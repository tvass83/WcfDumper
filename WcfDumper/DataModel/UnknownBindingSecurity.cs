namespace WcfDumper.DataModel
{
    /// <summary>
    /// Represents a bindings security unknown to this application.
    /// </summary>
    public class UnknownBindingSecurity : IBindingSecurity
    {
        private readonly string myBindingType = "Unknown";

        public UnknownBindingSecurity()
        {
        }

        public UnknownBindingSecurity(string bindingType)
        {
            if (bindingType != null)
            {
                myBindingType = bindingType;
            }
        }

        public override string ToString()
        {
            return $"Security Mode: {nameof(UnknownBindingSecurity)} for binding type {myBindingType}.";
        }
    }
}
