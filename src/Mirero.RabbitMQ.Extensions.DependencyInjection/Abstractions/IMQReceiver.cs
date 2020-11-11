namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;
    using System.Threading.Tasks;

    public interface IMQReceiver: IDisposable
    {
        void Ack();
        void Nack();
        Task<object> ReceiveAsync(TimeSpan timeout);
        Task<T> ReceiveAsync<T>(TimeSpan timeout);
        void Start(string topic);
    }
}
