namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Tests2
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using System.Threading;
    using FluentAssertions.Extensions;
    using FluentAssertions;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    public class TestAsyncSpec
    {
        [Fact]
        public async Task TestAsync()
        {
            var host = Host.CreateDefaultBuilder()
                           .ConfigureServices(services =>
                           {
                               services.AddRabbitMQ(model =>
                               {
                                   model.QueueDelete("mls.test.testservice", false, false);
                                   model.QueueDeclare("mls.test.testservice", false, false, false, null);
                               });
                           })
                           .Build();

            await host.StartAsync();

            using (var channel = host.Services.GetService<IMQPublisher>())
            {
                await channel.Tell("mls.test.testservice", new[] { "Hello", "World" });
                await channel.Tell("mls.test.testservice", new TestMessage("Hello"));
                await channel.Tell("mls.test.testservice", new TestMessage("World"));
                await channel.Tell("mls.test.testservice", 1);
            }

            using (var receiver = host.Services.GetService<IMQReceiver>())
            {
                receiver.Start("mls.test.testservice");

                {
                    var (message, commit) = await receiver.ReceiveAsync<IEnumerable<string>>(2.Seconds());
                    message.Should().BeSubsetOf(new[] {"Hello", "World"});

                    await commit.Ack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync(2.Seconds());
                    message.Should().BeOfType<TestMessage>();
                    message.As<TestMessage>().Value.Should().Be("Hello");

                    await commit.Nack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync(2.Seconds());
                    message.Should().BeOfType<TestMessage>();
                    message.As<TestMessage>().Value.Should().Be("Hello");
                    await commit.Ack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync<TestMessage2>(2.Seconds());
                    message.Value.Should().Be("World");
                    await commit.Ack();
                }

                {
                    var (message, commit) = await receiver.ReceiveAsync(2.Seconds());
                    message.As<long>().Should().Be(1);
                    await commit.Ack();
                }
            }

            await host.StopAsync(1.Seconds());
        }
    }
}
