namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System;
    using System.Threading.Tasks;

    public interface IMQPublisher : IDisposable
    {
        Task Tell(string topic, object message);
    }
}
