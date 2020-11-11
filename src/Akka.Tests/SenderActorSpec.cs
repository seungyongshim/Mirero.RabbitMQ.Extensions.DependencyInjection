namespace Akka.Tests
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Actor.Tests.Actors;
    using Akka.Actor;
    using Akka.DI.Core;
    using Akka.Tests.Actors;
    using FluentAssertions;
    using FluentAssertions.Extensions;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Xunit;

    public class SenderActorSpec : Akka.TestKit.Xunit2.TestKit
    {
        [Fact]
        public async Task Test1()
        {
            var host = Host.CreateDefaultBuilder()
                           .ConfigureServices(services =>
                           {
                               services.AddAkka(Sys, new[]
                               {
                                   "Actor.Tests",
                               });
                               services.AddRabbitMQ(model =>
                               {
                                   model.QueueDelete("rmq.test.akka.publisher", false, false);
                                   model.QueueDeclare("rmq.test.akka.publisher", false, false, false, null);
                               });
                           })
                           .Build();

            await host.StartAsync();

            host.Services.GetRequiredService<ActorSystem>();

            var probe = CreateTestProbe("probe");

            var receiverActor = probe.ChildActorOf(Sys.DI().PropsFactory<MQReceiverActor>().Create(), "ReceiverActor1");
            probe.ExpectMsg<MQReceiverActor.Created>(30.Seconds());

            var senderActor = probe.ChildActorOf(Sys.DI().PropsFactory<MQPublisherActor>().Create(), "SenderActor");
            probe.ExpectMsg<MQPublisherActor.Created>(30.Seconds());

            senderActor.Tell(new MQPublisherActor.Setup("rmq.test.akka.publisher"));

            senderActor.Tell("1");
            senderActor.Tell(new Hello());
            senderActor.Tell(new[] { "3" });
            senderActor.Tell("4");
            senderActor.Tell(new[] { "5" });

            probe.ExpectMsg<MQPublisherActor.Created>(30.Seconds());

            senderActor.Tell(new MQPublisherActor.Setup("rmq.test.akka.publisher"));

            receiverActor.Tell(new MQReceiverActor.Setup("rmq.test.akka.publisher"));

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
                m.Message.As<IEnumerable<string>>()
                         .Should()
                         .BeSubsetOf(new[] { "3" });
                s.Tell(new MQReceiverActor.Nack());
            });

            probe.ExpectMsg<MQReceiverActor.Received>((m, s) =>
            {
                m.Message.As<IEnumerable<string>>()
                         .Should()
                         .BeSubsetOf(new[] { "3" });
                s.Tell(new MQReceiverActor.Ack());
            });

            await host.StopAsync();
        }

        public class Hello
        {
            public bool IsMakeException { get; set; } = true;
        }
    }
}
