using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQRpcClient : IMQRpcClient
    {
        public MQRpcClient(IServiceProvider serviceProvider, IMQPublisher sender, ILogger<MQRpcClient> logger)
        {
            ServiceProvider = serviceProvider;
            Sender = sender;
            Logger = logger;
        }

        public IServiceProvider ServiceProvider { get; }
        public IMQPublisher Sender { get; }
        public ILogger<MQRpcClient> Logger { get; }

        public async Task<(T, ICommitable)> AskAsync<T>(string topic, object message, TimeSpan timeout)
        {
            var resQueueName = await Sender.Tell(topic, message, true);

            using (var receiver = ServiceProvider.GetService<IMQReceiver>())
            {
                receiver.StartListening(resQueueName);
                return await receiver.ReceiveAsync<T>(timeout);
            }
        }

        #region IDisposable Support

        private bool disposedValue = false; // 중복 호출을 검색하려면

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Sender.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
