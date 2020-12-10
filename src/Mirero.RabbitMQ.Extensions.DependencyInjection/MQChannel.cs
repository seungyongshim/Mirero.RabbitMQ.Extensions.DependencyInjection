using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using Abstractions;

    internal class MQChannel : IMQChannel
    {
        public MQChannel(IServiceProvider serviceProvider, ILogger<MQChannel> logger)
        {
            ServiceProvider = serviceProvider;
            Logger = logger;

            Model = ServiceProvider.GetService<IModel>();
            Model.BasicQos(0, 1, false);

            var props = Model.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 1;
            props.Expiration = "1800000";
            Props = props;
        }

        public IServiceProvider ServiceProvider { get; }
        public ILogger<MQChannel> Logger { get; }
        public IModel Model { get; }
        public IBasicProperties Props { get; private set; }

        public void BasicQueuePublish(string topic, byte[] body)
        {
            Model.BasicPublish("", topic, Props, body);
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
                    Model?.Dispose();
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
