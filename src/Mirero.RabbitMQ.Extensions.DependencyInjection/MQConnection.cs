using System;
using RabbitMQ.Client;
using Microsoft.Extensions.Options;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Options;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQConnection : IDisposable
    {
        public MQConnection(MQDeclares mqDeclares, IOptions<MQConnectionOptions> options)
        {
            MQDeclares = mqDeclares;
            MQConnectionOption = options.Value;
        }

        public IConnection Connection { get; private set; }
        public MQDeclares MQDeclares { get; }
        public MQConnectionOptions MQConnectionOption { get; }

        public void Connect()
        {
            var factory = new ConnectionFactory()
            {
                UseBackgroundThreadsForIO = true,
                AutomaticRecoveryEnabled = true,
                VirtualHost = MQConnectionOption.Vhost,
                UserName = MQConnectionOption.Username,
                Password = MQConnectionOption.Password,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                DispatchConsumersAsync = true,
            };

            var addresses = new[]
            {
                new AmqpTcpEndpoint("127.0.0.1"),
                new AmqpTcpEndpoint("localhost")
            };

            // RabbitMQ에 접속
            Connection = factory.CreateConnection(addresses);

            using (var model = CreateModel())
            {
                MQDeclares.Action.Invoke(model);
            }
        }

        public IModel CreateModel() => Connection.CreateModel();

        #region IDisposable Support

        private bool disposedValue = false; // 중복 호출을 검색하려면

        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection.Dispose();
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
