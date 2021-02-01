using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using ConsoleAppWithActor;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Hocon.Extensions.Configuration;
using Xunit;
using Mirero.Akka.Extensions.DependencyInjection;
using Akka.DependencyInjection;

namespace Akka.Tests
{
    public class MQReceiverActorSpec 
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
                               services.AddSingleton(sp =>
                                   new Akka.TestKit.Xunit2.TestKit(BootstrapSetup.Create()
                                                     .And(ServiceProviderSetup.Create(sp))));

                               services.AddSingleton(sp => sp.GetService<Akka.TestKit.Xunit2.TestKit>().Sys);

                               services.AddRabbitMQ(context.Configuration, model =>
                               {
                                   model.QueueDelete(topicName, false, false);
                                   model.QueueDeclare(topicName, false, false, false, null);
                               });
                           })
                           .UseAkka(sys =>
                           {

                           })
                           .Build();

            await host.StartAsync();
            var testKit = host.Services.GetService<Akka.TestKit.Xunit2.TestKit>();

            var probe = testKit.CreateTestProbe();

            var receiverActor = probe.ChildActorOf(testKit.Sys.PropsFactory<MQReceiverActor>().Create(), "ReceiverActor");
            probe.ExpectMsg<MQReceiverActor.Created>(3.Seconds());

            var senderActor = probe.ChildActorOf(testKit.Sys.PropsFactory<MQPublisherActor>().Create(), "SenderActor");
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
