using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using Xunit;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Tests2
{
    public class TimeoutSpec
    {
        [Fact]
        public async Task TimeoutTestAsync()
        {
            const string topicName = "mls.test.TimeoutSpec";

            var host = Host.CreateDefaultBuilder()
                               .ConfigureServices(services =>
                               {
                                   services.AddRabbitMQ(model =>
                                   {
                                       model.QueueDelete(topicName, false, false);
                                       model.QueueDeclare(topicName, false, false, false, null);
                                   });
                               })
                               .Build();

            await host.StartAsync();

            using (var receiver = host.Services.GetService<IMQReceiver>())
            {
                receiver.Start(topicName);

                Func<Task> throwExceptionAsync = async () => await receiver.ReceiveAsync<int>(2.Seconds());

                await throwExceptionAsync.Should()
                                         .ThrowAsync<OperationCanceledException>();

                using (var channel = host.Services.GetService<IMQPublisher>())
                {
                    channel.Tell(topicName, 1);
                }

                var result = await receiver.ReceiveAsync<int>(2.Seconds());

                result.Should().Be(1);
            }

            await host.StopAsync();
        }
    }
}
