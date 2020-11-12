namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;

    public interface IMQPublisher : IDisposable
    {
        void Tell(string topic, object message);
    }
}
