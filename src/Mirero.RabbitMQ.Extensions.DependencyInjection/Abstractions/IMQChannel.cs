using System;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{

    public interface IMQChannel : IDisposable
    {
        void BasicQueuePublish(string topic, byte[] body);
    }

}
