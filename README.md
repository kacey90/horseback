# Horseback
A simple message bus wrapper for distributed applications on .NET <br/><br/>
Horseback intends to enable developers build distributed applications using a variety of queuing systems, including Azure Service Bus, RabbitMQ, AWS SQS and SNS, and more.<br/><br/>
# Features
* **Pluggable Architecture:** Horseback's pluggable architecture allows it to be extended to support a variety of queuing systems.
* **Simple API:** Horseback provides a simple, consistent API/Interfaces for interacting with queuing systems, regardless of the underlying technology.
* **Async Support:** Horseback supports asynchronous messaging, allowing your applications to scale and perform efficiently.
* **Inbox Pattern Support:** Out of the box support for Inbox message pattern to ensure idempotency consumer/subscriber pattern in handling of messages.
* **Error Handling:** Horseback supports error handling policies for consumers/receivers<br/><br/>
# Getting Started
## Installation
You can install Horseback and install the package of the queuing system extension of choice using NuGet: <br/><br/>
`Install-Package Horseback.Core`
`Install-Package Horseback.Applications.AzureServiceBus`<br/>
## Usage
Here is how you add horseback functionality to your application in `ConfigureServices()` of `program.cs` or `startup.cs`<br/>
```c#
services
  .AddHorseback()
  .AddAzureServiceBus(connectionString: connectionString, topicName: "topic1");
```
To add subscribers (Receivers):
```c#
services.AddHorseback()
  .AddAzureServiceBus(connectionString: connectionString, topicName: "topic1")
  .AddReceiver<SampleMessage, SampleMessageHandler>(messageAction: nameof(SampleMessage))
  .AddReceiver<OrderSentMessage, OrderSentMessageHandler>(messageAction: nameof(OrderSentMessage));
```
To add support for Inbox message pattern:
```c#
services.AddHorseback()
  .AddInboxMessagePattern(databaseConnection: dbConnectionString, tableName: "InboxMessages")
  .AddAzureServiceBus(connectionString: connectionString, topicName: "topic1")
  .AddReceiver<SampleMessage>(messageAction: nameof(SampleMessage))
  .AddReceiver<OrderSentMessage>(messageAction: nameof(OrderSentMessage));
```
Initialize Service Bus on horseback and get the receivers listening for events by calling this method in your `Configure()` method or within `main()` method of `program.cs`
```c#
host.InitializeAzureServiceBus<Program>(logger: logger).GetAwaiter().GetResult();
```
To publish a message to the queue, inject `IMessagePublisher` and call the `publish()` method:
```c#
private readonly IMessagePublisher _messagePublisher;

public PublisherSample(IMessagePublisher messagePublisher)
{
    _messagePublisher = messagePublisher;
}

public async Task PublishMessage()
{
    SampleMessage message = new(Guid.NewGuid(), DateTime.Now, $"Hello from the other side of {typeof(PublisherSample).Assembly.FullName}");
    await _messagePublisher.Publish(message);
}
```
# Roadmap
Include
- [x] A readme file
- [x] Azure Service Bus Implementation
- [ ] RabbitMQ Implementation
- [ ] AWS SQS/SNS Implementation
- [ ] Kafka Implementation
- [ ] Redis Implementation
- [ ] Google pubsub Implmentation<br/><br/>

# Contributing
The open source community is an incredible place to learn, be inspired, and innovate, largely because of contributions from people like you. We truly appreciate any and all contributions you may make to this project.

If you have any ideas or suggestions for improving the project, please consider submitting a pull request. Alternatively, you can also open an issue and tag it as an "enhancement". And if you find our project useful, we would love for you to give it a star! Thank you once again for your support.

# License
Horseback is licensed under the MIT License.
