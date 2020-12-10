namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;
    using System.Threading.Tasks;

    public interface IMQReceiver: IDisposable
    {
        Task<(object, ICommitable)> ReceiveAsync(TimeSpan timeout);
        Task<(T, ICommitable)> ReceiveAsync<T>(TimeSpan timeout);
        void Start(string topic);
    }
}
