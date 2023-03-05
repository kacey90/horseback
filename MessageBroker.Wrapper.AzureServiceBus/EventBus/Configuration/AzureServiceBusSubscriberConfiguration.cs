using MessageBroker.Wrapper.Core.EventBus.Config;

namespace MessageBroker.Wrapper.AzureServiceBus.EventBus.Configuration
{
    public class AzureServiceBusSubscriberConfiguration : SubscriberConfiguration
    {
        public string TopicName { get; }
        public int MaxConcurrentCalls { get; set; }
        public int CustomRetryCount { get; set; }
        public int CustomRetryDelay { get; set; }
        //public int PrefetchCount { get; set; }

        private const int MaxConcurrentCallDefault = 1;
        private const bool AutoCompleteDefault = false;
        private const int CustomRetryCountDefault = 3;
        private const int CustomRetryDelayDefault = 10;
        //private const int PrefetchCountDefault = 1;

        public AzureServiceBusSubscriberConfiguration(
            string connectionString,
            string topicName,
            int maxConcurrentCalls,
            int customRetryCount,
            int customRetryDelay,
            bool autoCompleteMessage) : base(connectionString, autoCompleteMessage)
        {
            TopicName = topicName;
            MaxConcurrentCalls = maxConcurrentCalls;
            CustomRetryCount = customRetryCount;
            CustomRetryDelay = customRetryDelay;
        }

        public AzureServiceBusSubscriberConfiguration(string connectionString, string topicName, int maxConcurrentCalls, bool autoCompleteMessage)
            : this(connectionString, topicName, maxConcurrentCalls, CustomRetryCountDefault, CustomRetryDelayDefault, autoCompleteMessage)
        {
        }


        public AzureServiceBusSubscriberConfiguration(string connectionString, string topicName)
            : this(connectionString, topicName, MaxConcurrentCallDefault, CustomRetryCountDefault, CustomRetryDelayDefault, AutoCompleteDefault)
        {
        }
    }
}
