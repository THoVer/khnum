﻿using System;
using System.Threading.Tasks;
using Khnum.PostgreSql;
using Khnum.Contracts;
using Khnum.Tests.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Khnum.Publisher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var collection = new ServiceCollection();

            var registry = new ConsumerRegistry(collection);
            registry.Subscribe<SendTimeRequest, SendTimeRequestConsumer>("consumer-id-x");
            registry.Subscribe<SendTimeRequest, SendTimeRequestConsumer>("consumer-id-y");

            collection.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug).AddConsole());

            collection.AddSingleton<ISerializer, Serializer>();
            collection.AddSingleton<IConsumerRegistry>(registry);
            collection.AddSingleton<IBus, PostgreSqlBus>();
            collection.AddSingleton(new PostgreSqlBusOptions
            {
                ConnectionString = "Server=127.0.0.1;Database=test;User Id=admin;Password=admin"
            });
            collection.AddSingleton<IPublisher, PostgreSqlPublisher>();

            var services = collection.BuildServiceProvider(true);

            //await StartConsumer(services).ConfigureAwait(false);

            while (true)
            {
                Console.WriteLine("Press ESC to exit or any other key to publish");
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.Escape)
                    break;

                var publisher = services.GetRequiredService<IPublisher>();
                await publisher.PublishAsync(new SendTimeRequest {Time = DateTime.Now}).ConfigureAwait(false);
            }
        }

        private static async Task StartConsumer(IServiceProvider services)
        {
            var registry = services.GetRequiredService<IConsumerRegistry>();
            var bus = services.GetRequiredService<IBus>();
            using (var service = registry.CreateService(bus))
            {
                await service
                    .StartConsumersAsync(services)
                    .ConfigureAwait(false);
            }
        }
    }
}
