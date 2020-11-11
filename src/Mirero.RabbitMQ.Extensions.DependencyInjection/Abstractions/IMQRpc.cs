using System;
using System.Threading.Tasks;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    public interface IMQRpc
    {
        Task<object> ReceiveAsync(TimeSpan timeout);
        Task<T> AskAsync<T>(string topic, object message, TimeSpan timeout);
    }
}
