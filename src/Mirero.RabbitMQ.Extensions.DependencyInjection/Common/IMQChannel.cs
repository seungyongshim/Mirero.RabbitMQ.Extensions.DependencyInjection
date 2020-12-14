using System;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Common
{
    public interface IMQChannel : IDisposable
    {
        string BasicQueuePublish(string topic, byte[] body, bool expectResponce = false);
    }
}
