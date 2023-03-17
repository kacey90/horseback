
using Microsoft.Extensions.Hosting;
using Horseback.Applications.AzureServiceBus.EventBus;
using SampleReceiverConsole;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Horseback.Core.EventBus.Extensions;

var host = CreateHostBuilder(args).Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
host.InitializeAzureServiceBus<Program>(logger: logger).GetAwaiter().GetResult();
Console.ReadLine();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostContext, config) =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddUserSecrets<Program>();
        })
        .ConfigureServices((hostContext, services) =>
        {
            string connectionString = hostContext.Configuration.GetConnectionString("AzureServiceBus") ?? "your-connection";
            services.AddHorseback()
                .AddAzureServiceBus(connectionString, "Sample.Broker")
                .AddReceiver<SampleMessage, SampleMessageHandler>(nameof(SampleMessage))
                .AddReceiver<OrderSentMessage, OrderSentMessageHandler>(nameof(OrderSentMessage));

            //services.AddHorseback()
            //    .AddInboxMessagePattern(hostContext.Configuration.GetConnectionString("DbConnection"), "InboxMessages")
            //    .AddAzureServiceBus(connectionString, "Sample.Broker")
            //    .AddReceiver<SampleMessage>(nameof(SampleMessage))
            //    .AddReceiver<OrderSentMessage>(nameof(OrderSentMessage));
        });

