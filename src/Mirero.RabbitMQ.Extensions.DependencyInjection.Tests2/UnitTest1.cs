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

    public class UnitTest1
    {
        [Fact]
        public async Task Test1Async()
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

            using (var channel = host.Services.GetService<IMQChannel>())
            using (var cts = new CancellationTokenSource())
            {
                channel.Tell("mls.test.testservice", new[] { "Hello", "World" });
                channel.Tell("mls.test.testservice", new TestMessage("Hello"));
                channel.Tell("mls.test.testservice", new TestMessage("World"));
                channel.Tell("mls.test.testservice", new[] { "Hello", "World" });
            }

            using (var channel = host.Services.GetService<IMQChannel>())
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(60.Seconds());

                var message = await channel.ReceiveAsync<IEnumerable<string>>("mls.test.testservice", cts.Token);
                message.Should().BeSubsetOf(new[] { "Hello", "World" });

                channel.Ack();


                var message1 = await channel.ReceiveAsync("mls.test.testservice", cts.Token);
                message1.Should().BeOfType<TestMessage>();
                message1.As<TestMessage>().Value.Should().Be("Hello");

                channel.Ack();


                var message2 = await channel.ReceiveAsync<TestMessage2>("mls.test.testservice", cts.Token);
                message2.Value.Should().Be("World");

                channel.Ack();


                var message3 = await channel.ReceiveAsync<IEnumerable<string>>("mls.test.testservice", cts.Token);
                message3.Should().BeSubsetOf(new[] { "Hello", "World" });

                channel.Ack();
            }

            await host.StopAsync(1.Seconds());
        }
    }
}
