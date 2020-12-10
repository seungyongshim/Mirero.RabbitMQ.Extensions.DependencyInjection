using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace Akka.Tests.Actors
{
    public class MQPublisherActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public MQPublisherActor(IMQPublisher mqPublisher)
        {
            MQPublisher = mqPublisher;

            Receive<Setup>(Handle, null);
            ReceiveAny(_ => Stash.Stash());
            Context.Parent.Tell(new Created());
        }

        public IMQPublisher MQPublisher { get; }

        public IStash Stash { get; set; }

        public string Topic { get; private set; }

        protected override void PreRestart(Exception reason, object message)
        {
            _logger.Error(reason, message.ToString());

            // 테스트를 위한 코드
            // 메시지를 정상적으로 처리할 수 있게 메시지를 고친다.
            if (message is Akka.Tests.MQReceiverActorSpec.Hello a)
            {
                a.IsMakeException = false;

                Stash.Prepend(new[]
                {
                    new Envelope(a, Self)
                });
            }

            MQPublisher.Dispose();
            base.PreRestart(reason, message);
        }

        private void Handle(Setup msg)
        {
            Topic = msg.Topic;
            Become(RegisterMessageHandlers);
            Stash.UnstashAll();
        }

        private async Task HandleAsync(object msg)
        {
            await MQPublisher.Tell(Topic, msg);
        }

        private async Task HandleAsync(MQReceiverActorSpec.Hello msg)
        {
            if (msg.IsMakeException)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                throw new Exception();
            }

            await MQPublisher.Tell(Topic, msg);
        }

        private void RegisterMessageHandlers()
        {
            ReceiveAsync<MQReceiverActorSpec.Hello>(HandleAsync);
            ReceiveAsync<object>(HandleAsync);
        }

        public class Created { }

        public class Setup
        {
            public Setup(string topic) => Topic = topic;

            public string Topic { get; }
        }
    }
}
