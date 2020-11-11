namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IMQChannel : IDisposable
    {
        void Tell(string topic, object message);
        Task<T> ReceiveAsync<T>(string topic, CancellationToken ct);
        Task<object> ReceiveAsync(string topic, CancellationToken ct);
        void Ack();
    }
}
