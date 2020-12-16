using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace ConsoleAppWithActor
{
    public class MQReceiverActor : ReceiveActor, IWithUnboundedStash
    {
        public MQReceiverActor(IMQReceiver mqReceiver)
        {
            MQReceiver = mqReceiver;

            Receive<Setup>(Handle, null);
            ReceiveAny(_ => Stash.Stash());
            Context.Parent.Tell(new Created());
        }

        private interface IAckCommand
        {
            void Execute(object parameter);
        }

        public IMQReceiver MQReceiver { get; }
        public IStash Stash { get; set; }
        public string Topic { get; private set; }
        private ILoggingAdapter Logger { get; } = Context.GetLogger();

        private void Handle(Setup msg)
        {
            MQReceiver.StartListening(msg.Topic);
            Become(RegisterMessageHandlers);

            Stash.UnstashAll();
        }

        private async Task HandleAsync(Read arg)
        {
            var parent = Context.Parent;
            var self = Context.Self;

            var (ret, commit) = await MQReceiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            var ack = await parent.Ask<IAckCommand>(new Received(ret), TimeSpan.FromSeconds(30)).ConfigureAwait(false);

            ack.Execute(commit);

            self.Tell(new Read());
        }

        private void RegisterMessageHandlers()
        {
            ReceiveAsync<Read>(HandleAsync);
            Self.Tell(new Read());
        }

        public class Ack : IAckCommand
        {
            public void Execute(object parameter) => (parameter as ICommitable).Ack();
        }

        public class Created { }

        public class Nack : IAckCommand
        {
            public void Execute(object parameter) => (parameter as ICommitable).Nack();
        }

        public class Read { };

        public class Received
        {
            public Received(object message) => Message = message;

            public object Message { get; set; }
        }

        public class Setup
        {
            public Setup(string topic) => Topic = topic;

            public string Topic { get; set; }
        }
    }
}
