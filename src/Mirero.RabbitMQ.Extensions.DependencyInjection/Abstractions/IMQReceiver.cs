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
        /// <summary>
        /// http://wish.mirero.co.kr/mirero/project/mls/1.0/h18-mirero-mls10-rd/mls-application/-/issues/1649#note_178824
        /// </summary>
        /// <param name="topic"></param>
        void Start(string topic);
    }
}
