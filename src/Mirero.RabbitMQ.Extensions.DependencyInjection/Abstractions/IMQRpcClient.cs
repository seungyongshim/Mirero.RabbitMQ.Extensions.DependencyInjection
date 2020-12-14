using System;
using System.Threading.Tasks;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    public interface IMQRpcClient
    {
        Task<(T, ICommitable)> AskAsync<T>(string topic, object message, TimeSpan timeout);
    }
}
