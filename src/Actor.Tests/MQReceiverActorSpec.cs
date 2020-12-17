using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.DI.Core;
using ConsoleAppWithActor;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Hocon.Extensions.Configuration;
using Xunit;

namespace Akka.Tests
{
    public class MQReceiverActorSpec : TestKit.Xunit2.TestKit
    {
        [Fact]
        public async Task Test1()
        {
            const string topicName = "rmq.test.akka";

            var host = Host.CreateDefaultBuilder()
                           .ConfigureHostConfiguration(config =>
                           {
                               config.AddHoconFile("test.hocon");
                           })
                           .ConfigureServices((context,services) =>
                           {
                               services.AddAkka(Sys);

                               services.AddRabbitMQ(context.Configuration, model =>
                               {
                                   model.QueueDelete(topicName, false, false);
                                   model.QueueDeclare(topicName, false, false, false, null);
                               });
                           })
                           .Build();

            await host.StartAsync();

            var probe = CreateTestProbe();

            var receiverActor = probe.ChildActorOf(Sys.DI().PropsFactory<MQReceiverActor>().Create(), "ReceiverActor");
            probe.ExpectMsg<MQReceiverActor.Created>(3.Seconds());

            var senderActor = probe.ChildActorOf(Sys.DI().PropsFactory<MQPublisherActor>().Create(), "SenderActor");
            probe.ExpectMsg<MQPublisherActor.Created>(3.Seconds());

            senderActor.Tell(new MQPublisherActor.Setup(topicName));

            senderActor.Tell("1");
            senderActor.Tell(new Hello());
            senderActor.Tell(new[] { 3 });
            senderActor.Tell("4");
            senderActor.Tell(new[] { "5" });

            receiverActor.Tell(new MQReceiverActor.Setup(topicName));

            probe.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<string>().Should().Be("1");
                s.Tell(new MQReceiverActor.Ack());
            }, 5.Seconds());

            probe.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<Hello>();
                s.Tell(new MQReceiverActor.Ack());
            });

            probe.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<IEnumerable<int>>()
                         .Should()
                         .BeSubsetOf(new[] { 3 });
                s.Tell(new MQReceiverActor.Nack());
            });

            probe.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<IEnumerable<int>>()
                         .Should()
                         .BeSubsetOf(new[] { 3 });
                s.Tell(new MQReceiverActor.Ack());
            });

            await host.StopAsync();
        }

        public class Hello { }
    }
}
