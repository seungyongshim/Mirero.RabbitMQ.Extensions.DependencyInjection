namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using Newtonsoft.Json;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;

    public class MQChannel : IMQChannel
    {
        private bool disposedValue = false;
        private IModel _model;

        public MQChannel(IServiceProvider serviceProvider, ILogger<MQChannel> logger)
        {
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        public IModel Model => _model ?? ServiceProvider.GetService<IModel>();
        public IServiceProvider ServiceProvider { get; }
        public ILogger<MQChannel> Logger { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<T> ReceiveAsync<T>(string topic, CancellationToken ct)
        {
            var ch = Channel.CreateUnbounded<string>();
            var consumer = new AsyncEventingBasicConsumer(Model);
            consumer.Received += Consumer_Received;

            try
            {
                Model.BasicQos(0, 1, false);
                Model.BasicConsume(topic, false, consumer);
                
                var rawMessage = await ch.Reader.ReadAsync(ct);

                var result = JsonConvert.DeserializeObject<T>(rawMessage, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                _model = null;
            }
            finally
            {
                consumer.Received -= Consumer_Received;
            }

            return default;

            async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
            {
                try
                {
                    //.Net5 전환시 ToArray를 Span으로 수정할 것
                    var rawMessage = Encoding.UTF8.GetString(e.Body.ToArray());
                    await ch.Writer.WriteAsync(rawMessage).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ch.Writer.Complete(ex);
                }
            }
        }

        public void Send(string topic, object message, string topicType)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerialize(message));
            var props = Model.CreateBasicProperties();
                props.ContentType = "application/json";
                props.DeliveryMode = 1;
                props.Expiration = "100000";

            try
            {
                Model.BasicPublish("", topic, props, body);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "");
                _model = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Model.Dispose();
                }
                disposedValue = true;
            }
        }

        private string JsonSerialize(object message)
        {
            try
            {
                var result = JsonConvert.SerializeObject(message, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Full
                });
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                return null;
            }
        }
    }
}
