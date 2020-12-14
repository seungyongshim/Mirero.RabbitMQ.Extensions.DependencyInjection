namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions
{
    using System.Threading.Tasks;

    public interface ICommitable
    {
        string ReplyTo { get; }

        Task Ack();

        Task Nack();
    }
}
