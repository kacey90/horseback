// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Horseback.Applications.AzureServiceBus.EventBus;
using SamplePulisherConsole;
using Microsoft.Extensions.Configuration;
using Horseback.Core.EventBus.Extensions;

var host = CreateHostBuilder(args).Build();


//for (int i = 0; i < 5; i++)
//{
//    var publisher = host.Services.GetRequiredService<PublisherSample>();
//    await publisher.PublishMessage();
//    Console.WriteLine("Message published - {0}", i);
//    await Task.Delay(2000);
//}

while (true)
{
    // make an interactive session to chose which message to run
    Console.WriteLine("Choose a message to publish:");
    Console.WriteLine("1. SampleMessage");
    Console.WriteLine("2. OrderSentMessage");
    Console.WriteLine("3. Exit");
    var key = Console.ReadKey();
    Console.WriteLine();
    switch (key.KeyChar)
    {
        case '1':
            var publisher = host.Services.GetRequiredService<PublisherSample>();
            await publisher.PublishMessage();
            break;
        case '2':
            var orderPublisher = host.Services.GetRequiredService<PublishOrder>();
            Console.WriteLine("Publishing message...");
            await orderPublisher.PublishOrderMessage();
            Console.WriteLine("Message published");
            break;
        case '3':
            return;
        default:
            Console.WriteLine("Invalid option");
            break;
    }
}

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
            services
                .AddHorseback()
                .AddAzureServiceBus(connectionString, "Sample.Broker");

            services.AddTransient<PublisherSample>();
            services.AddTransient<PublishOrder>();
        });
