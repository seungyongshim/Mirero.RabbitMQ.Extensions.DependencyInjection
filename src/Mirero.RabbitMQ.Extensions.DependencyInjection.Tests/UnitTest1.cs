namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Tests
{
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using FluentAssertions.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using Xunit;

    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var builder = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseKestrel(option => option.Listen(IPAddress.IPv6Loopback, 0))
                .ConfigureServices(services =>
                {
                    services.AddRabbitMQ(model =>
                    {
                        model.QueueDeclare("rmq.test.test1", false, false, true, null);
                    });
                });

            var host = builder.Build();

            await host.StartAsync();

            using (var sender = host.Services.GetService<IMQPublisher>())
            using (var cts = new CancellationTokenSource())
            {
                await sender.Tell("rmq.test.test1", "Hello");
            }

            using (var receiver = host.Services.GetService<IMQReceiver>())
            using (var cts = new CancellationTokenSource())
            {
                receiver.Start("rmq.test.test1");
                var (message, commit)= await receiver.ReceiveAsync<string>(5.Seconds());
                message.Should().Be("Hello");
                await commit.Ack();
            }

            await host.StopAsync(1.Seconds());
        }
    }
}
