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
                channel.Tell("mls.test.testservice", new[] { "Hello", "World" });
                channel.Tell("mls.test.testservice", new TestMessage("Hello"));
                channel.Tell("mls.test.testservice", new TestMessage("World"));
                channel.Tell("mls.test.testservice", 1);
            }

            using (var receiver = host.Services.GetService<IMQReceiver>())
            {
                receiver.Start("mls.test.testservice");

                var message = await receiver.ReceiveAsync<IEnumerable<string>>(2.Seconds());
                message.Should().BeSubsetOf(new[] { "Hello", "World" });

                receiver.Ack();


                var message1 = await receiver.ReceiveAsync(2.Seconds());
                message1.Should().BeOfType<TestMessage>();
                message1.As<TestMessage>().Value.Should().Be("Hello");

                receiver.Nack();


                var message1_1 = await receiver.ReceiveAsync(2.Seconds());
                message1_1.Should().BeOfType<TestMessage>();
                message1_1.As<TestMessage>().Value.Should().Be("Hello");

                receiver.Ack();

                var message2 = await receiver.ReceiveAsync<TestMessage2>(2.Seconds());
                message2.Value.Should().Be("World");

                receiver.Ack();


                var message3 = await receiver.ReceiveAsync(2.Seconds());
                message3.As<long>().Should().Be(1);

                receiver.Ack();
            }

            await host.StopAsync(1.Seconds());
        }
    }
}
