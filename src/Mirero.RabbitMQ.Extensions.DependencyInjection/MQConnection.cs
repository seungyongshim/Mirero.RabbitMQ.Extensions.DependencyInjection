using System;
using RabbitMQ.Client;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQConnection : IDisposable
    {
        public MQConnection(MQDeclares mqDeclares) => MQDeclares = mqDeclares;

        public IConnection Connection { get; private set; }
        public MQDeclares MQDeclares { get; }

        public void Connect()
        {
            var factory = new ConnectionFactory()
            {
                UseBackgroundThreadsForIO = true,
                AutomaticRecoveryEnabled = true,
                VirtualHost = "/",
                UserName = "mirero",
                Password = "system",
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            var addresses = new AmqpTcpEndpoint[]
            {
                new AmqpTcpEndpoint("127.0.0.1")
            };

            // RabbitMQ에 접속
            Connection = factory.CreateConnection(addresses);

            using (var model = CreateModel())
            {
                MQDeclares.Action.Invoke(model);
            }
        }

        public void Close()
        {
            Connection.Close();
        }

        public IModel CreateModel()
        {
            if (Connection != null)
            {
                if (Connection.IsOpen)
                {
                    var model = Connection.CreateModel();
                    return model;
                }
            }
            return null;
        }

        #region IDisposable Support

        private bool disposedValue = false; // 중복 호출을 검색하려면

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
