
using Microsoft.Extensions.Hosting;
using MessageBroker.Wrapper.AzureServiceBus.EventBus;
using SampleReceiverConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var host = CreateHostBuilder(args).Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
host.InitializeAzureEventSubscribers(logger);
Console.ReadLine();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.AddAzureServiceBus("Endpoint=sb://thirdpartypartialdebit.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=HvfWutG40Qu52i0txPUM9EH3K2G0B0TiEvLm1669nDs=", "Sample.Broker")
            .AddReceiver<SampleMessage, SampleMessageHandler>(nameof(SampleMessage))
            .AddReceiver<OrderSentMessage, OrderSentMessageHandler>(nameof(OrderSentMessage));
        });

