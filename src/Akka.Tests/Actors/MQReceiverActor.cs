namespace Actor.Tests.Actors
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using Akka.Actor;
    using Akka.Event;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

    public class MQReceiverActor : ReceiveActor, IWithUnboundedStash
    {
        private readonly ILoggingAdapter _logger = Context.GetLogger();

        public MQReceiverActor(IMQReceiver mqReceiver)
        {
            MQReceiver = mqReceiver;

            Receive<Setup>(Handle, null);
            ReceiveAny(_ => Stash.Stash());
            Context.Parent.Tell(new Created());
        }

        public IMQReceiver MQReceiver { get; }

        public IStash Stash { get; set; }

        public string Topic { get; private set; }

        private void Handle(Setup msg)
        {
            MQReceiver.Start(msg.Topic);
            Become(RegisterMessageHandlers);

            Stash.UnstashAll();
        }

        private async Task HandleAsync(Read arg)
        {
            var parent = Context.Parent;
            var self = Context.Self;

            var (ret, commit) = await MQReceiver.ReceiveAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            var ack = await parent.Ask<ICommand>(new Received(ret), TimeSpan.FromSeconds(30)).ConfigureAwait(false);

            ack.Execute(commit);

            self.Tell(new Read());
        }

        private void RegisterMessageHandlers()
        {
            ReceiveAsync<Read>(HandleAsync);
            Self.Tell(new Read());
        }

        public class Ack : ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => throw new NotImplementedException();

            public void Execute(object parameter) => (parameter as ICommitable).Ack();
        }

        public class Created { }

        public class Nack : ICommand
        {
            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => throw new NotImplementedException();

            public void Execute(object parameter) => (parameter as ICommitable).Nack();
        }

        public class Read { };

        public class Received
        {
            public Received(object message)
            {
                Message = message;
            }

            public object Message { get; set; }
        }

        public class Setup
        {
            public Setup(string topic) => Topic = topic;

            public string Topic { get; set; }
        }
    }
}
