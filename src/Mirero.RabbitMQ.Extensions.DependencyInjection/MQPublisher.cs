namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::RabbitMQ.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using Newtonsoft.Json;

    public class MQPublisher : IMQPublisher
    {
        public MQPublisher(IServiceProvider serviceProvider, IMQChannel channel, ILogger<MQPublisher> logger )
        {
            Logger = logger;
            Channel = channel;
        }

        public ILogger<MQPublisher> Logger { get; }
        public IMQChannel Channel { get; }

        public Task Tell(string topic, object message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerialize(message));
                Channel.BasicQueuePublish(topic, body);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                return Task.FromException(ex);
            }

            string JsonSerialize(object msg)
            {
                var result = JsonConvert.SerializeObject(msg, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                });
                return result;
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
                    Channel?.Dispose();
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
