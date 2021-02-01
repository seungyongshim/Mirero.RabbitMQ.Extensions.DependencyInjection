using System;
using System.Threading.Tasks;
using Akka.Actor;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace ConsoleAppWithActor
{
    public class MQReceiverActor : ReceiveActor
    {
        public MQReceiverActor(IMQReceiver mqReceiver, string topic, IActorRef parserActor)
        {
            ParserActor = parserActor;
            MQReceiver = mqReceiver;
            Topic = topic;

            RegisterMessageHandlers();

            Self.Tell(new StartListening());
        }

        public IMQReceiver MQReceiver { get; }
        public string Topic { get; }
        public IActorRef ParserActor { get; }

        private async Task HandleAsync(Read arg)
        {
            var parserActor = ParserActor;
            var self = Context.Self;

            var (ret, commit) = await MQReceiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            var ack = await parserActor.Ask<IAckCommand>(new Received(ret), TimeSpan.FromSeconds(30)).ConfigureAwait(false);

            ack.Execute(commit);

            self.Tell(new Read());
        }

        private void RegisterMessageHandlers()
        {
            ReceiveAsync<Read>(HandleAsync);
            Receive<StartListening>(Handle, null);
        }

        private void Handle(StartListening _)
        {
            MQReceiver.StartListening(Topic);
            Self.Tell(new Read());
        }

        private interface IAckCommand
        {
            void Execute(ICommitable commit);
        }

        public class Ack : IAckCommand
        {
            public void Execute(ICommitable commit) => commit.Ack();
        }

        public class Nack : IAckCommand
        {
            public void Execute(ICommitable commit) => commit.Nack();
        }

        public class Read { };
        public class StartListening { };

        public class Received
        {
            public Received(object message) => Message = message;

            public object Message { get; set; }
        }
    }
}
