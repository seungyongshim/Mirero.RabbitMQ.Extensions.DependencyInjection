namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;
    using System.Threading.Tasks;

    public interface IMQPublisher : IDisposable
    {
        Task<string> TellAsync(string topic, object message, bool expectResponse = false);
    }
}
