using System;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Event;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace ConsoleAppWithActor
{
    public class MQPublisherActor : ReceiveActor
    {
        public MQPublisherActor(IMQPublisher mqPublisher, string topic)
        {
            MQPublisher = mqPublisher;
            Topic = topic;
            RegisterMessageHandlers();
        }

        public IMQPublisher MQPublisher { get; }

        public IStash Stash { get; set; }

        public string Topic { get; }

        private async Task HandleAsync(object msg) => await MQPublisher.TellAsync(Topic, msg);

        private void RegisterMessageHandlers() => ReceiveAsync<object>(HandleAsync);
    }
}
