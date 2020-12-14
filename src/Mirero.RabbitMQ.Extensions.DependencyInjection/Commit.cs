using System;
using System.Threading.Tasks;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class Commit : ICommitable
    {
        public Commit(ulong deliveryTag, string replyTo, Func<ulong, Task> ackAction, Func<ulong, Task> nackAction)
        {
            AckAction = () => ackAction(deliveryTag);
            NackAction = () => nackAction(deliveryTag);
            ReplyTo = replyTo;
        }

        public string ReplyTo { get; }
        private Func<Task> AckAction { get; }
        private Func<Task> NackAction { get; }

        public async Task Ack() => await AckAction.Invoke();
        public async Task Nack() => await NackAction.Invoke();
    }
}
