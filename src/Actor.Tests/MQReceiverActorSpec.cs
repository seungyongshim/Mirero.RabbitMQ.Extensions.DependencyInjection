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

            var receiverActor = testKit.ActorOf(testKit.Sys.PropsFactory<MQReceiverActor>().Create(topicName, testKit.TestActor), "ReceiverActor");
            var senderActor = testKit.ActorOf(testKit.Sys.PropsFactory<MQPublisherActor>().Create(topicName), "SenderActor");

            senderActor.Tell("1");
            senderActor.Tell(new Hello());
            senderActor.Tell(new[] { 3 });
            senderActor.Tell("4");
            senderActor.Tell(new[] { "5" });

            testKit.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<string>().Should().Be("1");
                s.Tell(new MQReceiverActor.Ack());
            }, 5.Seconds());

            testKit.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<Hello>();
                s.Tell(new MQReceiverActor.Ack());
            });

            testKit.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<IEnumerable<int>>()
                         .Should()
                         .BeSubsetOf(new[] { 3 });
                s.Tell(new MQReceiverActor.Nack());
            });

            testKit.ExpectMsg<MQReceiverActor.Received>((m, s) =>
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
