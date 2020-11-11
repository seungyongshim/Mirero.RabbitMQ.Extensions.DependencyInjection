namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;

    public interface IMQSender : IDisposable
    {
        void Tell(string topic, object message);
    }
}
