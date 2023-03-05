﻿// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Hosting;
using MessageBroker.Wrapper.AzureServiceBus.EventBus;
using Microsoft.Extensions.DependencyInjection;
using SamplePulisherConsole;

var host = CreateHostBuilder(args).Build();

//Console.WriteLine("Waiting for 15 secs...");
//await Task.Delay(15000);

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
        .ConfigureServices((hostContext, services) =>
        {
            services.AddAzureServiceBus("Endpoint=sb://thirdpartypartialdebit.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=HvfWutG40Qu52i0txPUM9EH3K2G0B0TiEvLm1669nDs=", "Sample.Broker");
            services.AddTransient<PublisherSample>();
            services.AddTransient<PublishOrder>();
        });

//Console.WriteLine("Hello, World!");
