using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Hocon.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using Xunit;

namespace GeneralHost.Tests
{
    public class TimeoutSpec
    {
        [Fact]
        public async Task TimeoutTestAsync()
        {
            const string topicName = "mls.test.time-out";

            var host = Host.CreateDefaultBuilder()
                           .ConfigureAppConfiguration(config =>
                           {
                               config.AddHoconFile("test.hocon");
                           })
                           .ConfigureServices((context, services) =>
                           {
                               services.AddRabbitMQ(context.Configuration, model =>
                               {
                                   model.QueueDelete(topicName, false, false);
                                   model.QueueDeclare(topicName, false, false, false, null);
                               });
                           })
                           .Build();

            await host.StartAsync();

            using (var receiver = host.Services.GetService<IMQReceiver>())
            {
                receiver.StartListening(topicName);

                // Message가 없을 때 수신을 시도하는 경우 
                Func<Task> throwExceptionAsync = async () => await receiver.ReceiveAsync<int>(2.Seconds());
                await throwExceptionAsync.Should().ThrowAsync<OperationCanceledException>();
            }

            await host.StopAsync();
        }
    }
}
