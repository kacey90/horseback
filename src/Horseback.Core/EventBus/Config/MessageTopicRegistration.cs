using System;

namespace Horseback.Core.EventBus.Config
{
    public class MessageTopicRegistration
    {
        public MessageTopicRegistration(string topic, Type subscriberType) 
        {
            Topic = topic;
            SubscriberType = subscriberType;
        }
        public string Topic { get; }
        public Type SubscriberType { get; }
    }
}
