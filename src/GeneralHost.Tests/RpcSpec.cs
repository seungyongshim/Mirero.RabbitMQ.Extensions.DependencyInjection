using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using FluentAssertions.Extensions;
using System.Threading;
using FluentAssertions;
using Hocon.Extensions.Configuration;
using AutoFixture.Xunit2;

namespace GeneralHost.Tests
{
    public class RpcSpec
    {
        public class Request { }
        public class Response
        {
            public Guid Guid { get; set; }
        }

        [Theory]
        [InlineAutoData]
        [InlineAutoData]
        [InlineAutoData]
        [InlineAutoData]
        public async Task Default(Response messageFixture)
        {
            const string topicName = "test.mqrpcspec.default";

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

            var rpc = host.Services.GetService<IMQRpcClient>();

            var t = new Thread(async () =>
            {
                using (var receiver = host.Services.GetService<IMQReceiver>())
                {
                    receiver.StartListening(topicName);
                    var (request, c)= await receiver.ReceiveAsync<Request>(10.Seconds());

                    using (var sender = host.Services.GetService<IMQPublisher>())
                    {
                        await sender.TellAsync(c.ReplyTo, messageFixture);
                    }

                    await c.Ack();
                }
            });

            t.Start();

            var (res, commit) = await rpc.AskAsync<Response>(topicName, new Request { }, 10.Seconds());

            res.Guid.Should().Be(messageFixture.Guid);

            await host.StopAsync();
            await Task.Delay(500);
        }
    }
}
