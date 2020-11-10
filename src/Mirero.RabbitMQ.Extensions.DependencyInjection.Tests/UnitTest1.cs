using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using RabbitMQ.Client;
using Xunit;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Tests
{
    public class UnitTest1
    {
        [Fact]
        public async Task Test1()
        {
            var builder = Microsoft.AspNetCore.WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .UseKestrel(option =>
                {
                    option.Listen(IPAddress.IPv6Loopback, 0);
                })
                .ConfigureServices(services =>
                {
                    services.AddRabbitMQ(model =>
                    {
                        model.QueueDeclare("mls.test.test1.consumer1", false, false, true, null);
                    });
                });

            var host = builder.Build();

            host.Start();

            using (var scope = host.Services.CreateScope())
            using (var channel = scope.ServiceProvider.GetService<IMQChannel>())
            {
                channel.Send("mls.test.test1.consumer1", "Hello");
            }

            using (var scope = host.Services.CreateScope())
            using (var channel = scope.ServiceProvider.GetService<IMQChannel>())
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(3000.Microseconds());
                var message = await channel.ReceiveAsync<string>("mls.test.test1.consumer1", cts.Token);
                message.Should().Be("Hello");
            }

            await host.StopAsync(1.Seconds());
        }
    }
}
