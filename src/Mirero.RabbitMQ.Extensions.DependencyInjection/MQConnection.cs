namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using System;
    using global::RabbitMQ.Client;
    using Microsoft.Extensions.Options;

    public class MQConnection : IDisposable
    {
        public MQConnection(MQDeclares mqDeclares, IOptions<MQConnectionOption> options)
        {
            MQDeclares = mqDeclares;
            MQConnectionOption = options.Value;
        }

        public IConnection Connection { get; private set; }
        public MQDeclares MQDeclares { get; }
        public MQConnectionOption MQConnectionOption { get; }

        public void Connect()
        {
            var factory = new ConnectionFactory()
            {
                UseBackgroundThreadsForIO = MQConnectionOption.UseBackgroundThreadsForIO,
                AutomaticRecoveryEnabled = true,
                VirtualHost = "/",
                UserName = "mirero",
                Password = "system",
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
