using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Common;
using Newtonsoft.Json;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQPublisher : IMQPublisher
    {
        public MQPublisher(IMQChannel channel, ILogger<MQPublisher> logger)
        {
            Logger = logger;
            Channel = channel;
        }

        public ILogger<MQPublisher> Logger { get; }
        public IMQChannel Channel { get; }

        public async Task<string> TellAsync(string topic, object message, bool expectResponse = false)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerialize(message));
            var ret = Channel.BasicQueuePublish(topic, body, expectResponse);
            return await Task.FromResult(ret);

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
