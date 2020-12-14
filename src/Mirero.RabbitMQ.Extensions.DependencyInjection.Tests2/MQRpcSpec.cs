namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Tests2
{
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

    public class MQRpcSpec
    {
        class Request { }
        class Response { }

        [Fact]
        public async Task Default()
        {
            var host = Host.CreateDefaultBuilder()
                           .ConfigureServices(services =>
                           {
                               services.AddRabbitMQ(model =>
                               {
                                   model.QueueDelete("mls.test.testservice2", false, false);
                                   model.QueueDeclare("mls.test.testservice2", false, false, false, null);
                               });
                           })
                           .Build();

            await host.StartAsync();

            var rpc = host.Services.GetService<IMQRpc>();

            var t = new Thread(async () =>
            {
                using (var receiver = host.Services.GetService<IMQReceiver>())
                {
                    receiver.StartListening("mls.test.testservice2");
                    var (request, c)= await receiver.ReceiveAsync<Request>(10.Seconds());

                    using (var sender = host.Services.GetService<IMQPublisher>())
                    {
                        await sender.Tell(c.ReplyTo, new Response());
                    }

                    await c.Ack();
                }
            });

            t.Start();

            var (res, commit) = await rpc.AskAsync<Response>("mls.test.testservice2", new Request { }, 3.Seconds());

            res.Should().BeOfType<Response>();

            await host.StopAsync();
        }
    }
}
