using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQRpc : IMQRpc
    {
        public MQRpc(IMQSender sender, IMQReceiver receiver, ILogger<MQRpc> logger)
        {
            Sender = sender;
            Receiver = receiver;
            Logger = logger;
        }

        public IMQSender Sender { get; }
        public IMQReceiver Receiver { get; }
        public ILogger<MQRpc> Logger { get; }

        
        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        public Task<object> ReceiveAsync(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public Task<T> AskAsync<T>(string topic, object message, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Sender.Dispose();
                    Receiver.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

       
        #endregion
    }
}
