namespace Horseback.Core.EventBus.Config
{
    public abstract class SubscriberConfiguration
    {
        public bool AutoCompleteMessage { get; private set; }

        public string ConnectionString { get; private set; }

        public SubscriberConfiguration(string connectionString, bool autoCompleteMessage)
        {
            ConnectionString= connectionString;
            AutoCompleteMessage = autoCompleteMessage;
        }
    }
}
