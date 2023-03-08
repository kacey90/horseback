// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Hosting;
using MessageBroker.Wrapper.AzureServiceBus.EventBus;
using SampleReceiverConsole2;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

var host = CreateHostBuilder(args).Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
await host.InitializeAzureEventSubscribers<Program>(logger: logger);
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
            services.AddAzureServiceBus(connectionString, "Sample.Broker")
            .AddReceiver<OrderSentMessage, OrderSentMessageHandler>(nameof(OrderSentMessage));
        });
