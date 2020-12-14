using System;
using System.Threading.Tasks;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    public interface IMQRpc
    {
        Task<(T, ICommitable)> AskAsync<T>(string topic, object message, TimeSpan timeout);
    }
}
