using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Hocon.Extensions.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using Xunit;
using AutoFixture.Xunit2;

namespace AspNetCore.Tests
{
    public class HostWithAspNetCore
    {
        [Theory]
        [InlineAutoData]
        [InlineAutoData]
        [InlineAutoData]
        [InlineAutoData]
        public async Task SimplePubRec(string messageFixture)
        {
            const string topicName = "test.asp-net-core.simple";

            var builder = WebHost.CreateDefaultBuilder()
                                 .ConfigureAppConfiguration(config =>
                                 {
                                     config.AddHoconFile("test.hocon");
                                 })
                                 .UseStartup<Startup>()
                                 .UseKestrel(option => option.Listen(IPAddress.Loopback, 0))
                                 .ConfigureServices((context, services) =>
                                 {
                                     services.AddRabbitMQ(context.Configuration, model =>
                                     {
                                         model.QueueDelete(topicName, false, false);
                                         model.QueueDeclare(topicName, false, false, false, null);
                                     });
                                 });

            var host = builder.Build();

            await host.StartAsync();

            using (var sender = host.Services.GetService<IMQPublisher>())
            {
                await sender.TellAsync(topicName, messageFixture);
            }

            using (var receiver = host.Services.GetService<IMQReceiver>())
            {
                receiver.StartListening(topicName);
                var (message, commit) = await receiver.ReceiveAsync<string>(5.Seconds());
                message.Should().Be(messageFixture);
                await commit.Ack();
            }

            await host.StopAsync(1.Seconds());
            await Task.Delay(500);
        }
    }
}
