using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace ConsoleAppWithActor
{
    public class MQPublisherActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ILoggingAdapter Logger = Context.GetLogger();

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
            Logger.Error(reason, message.ToString());

            // 처리 못한 메시지를 다시 받는다.
            Stash.Prepend(new[]
            {
                new Envelope(message, Self)
            });

            MQPublisher.Dispose();
            base.PreRestart(reason, message);
        }

        private void Handle(Setup msg)
        {
            Topic = msg.Topic;
            Become(RegisterMessageHandlers);
            Stash.UnstashAll();
        }

        private async Task HandleAsync(object msg) => await MQPublisher.TellAsync(Topic, msg);

        private void RegisterMessageHandlers() => ReceiveAsync<object>(HandleAsync);

        public class Created { }

        public class Setup
        {
            public Setup(string topic) => Topic = topic;

            public string Topic { get; }
        }
    }
}
