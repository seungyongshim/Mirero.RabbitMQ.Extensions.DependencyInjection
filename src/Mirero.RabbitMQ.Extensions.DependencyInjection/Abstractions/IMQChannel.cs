using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    public interface IMQChannel : IDisposable
    {
        void Send(string topic, object message, string topicType = "fanout");
        Task<T> ReceiveAsync<T>(string topic, CancellationToken ct);
    }
}
