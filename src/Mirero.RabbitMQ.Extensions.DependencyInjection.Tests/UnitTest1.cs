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
                        model.QueueDeclare("mls.test.test1.consumer1", false, false, true, null);
                    });
                });

            var host = builder.Build();

            host.Start();

            using (var channel = host.Services.GetService<IMQChannel>())
            using (var cts = new CancellationTokenSource())
            {
                channel.Send("mls.test.test1.consumer1", "Hello");
            }

            using (var channel = host.Services.GetService<IMQChannel>())
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(555.Seconds());
                var message = await channel.ReceiveAsync<string>("mls.test.test1.consumer1", cts.Token);
                message.Should().Be("Hello");
            }

            await host.StopAsync(1.Seconds());
        }
    }
}
