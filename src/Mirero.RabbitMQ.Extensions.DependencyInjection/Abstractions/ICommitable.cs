namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System.Threading.Tasks;

    public interface ICommitable
    {
        Task Ack();

        Task Nack();
    }
}
