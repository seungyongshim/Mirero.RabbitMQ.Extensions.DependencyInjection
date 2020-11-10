using System;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQChannel : IMQChannel
    {
        private bool disposedValue = false;

        public MQChannel(IModel model, ILogger<MQChannel> logger)
        {
            Model = model;
            Logger = logger;
        }

        public IModel Model { get; }
        public ILogger<MQChannel> Logger { get; }
        public Lazy<IAsyncBasicConsumer> Consumer { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<T> ReceiveAsync<T>(string topic, CancellationToken ct)
        {
            Model.BasicQos(0, 1, false);

            var consumer = new EventingBasicConsumer(Model);
            Model.BasicConsume(topic, false, consumer);

            var ch = Channel.CreateUnbounded<string>();

            try
            {
                consumer.Received += Consumer_Received;
                var rawMessage = await ch.Reader.ReadAsync(ct);

                var result = JsonConvert.DeserializeObject<T>(rawMessage, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                return result;
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "");
            }
            finally
            {
                consumer.Received -= Consumer_Received;
            }

            return default(T);

            async void Consumer_Received(object sender, BasicDeliverEventArgs e)
            {
                try
                {
                    //.Net5 전환시 ToArray를 Span으로 수정할 것
                    var rawMessage = Encoding.UTF8.GetString(e.Body.ToArray()); 
                    await ch.Writer.WriteAsync(rawMessage);
                }
                catch(Exception ex)
                {
                    ch.Writer.TryComplete(ex);
                }
            }
        }

        public void Send(string topic, object message, string topicType)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerialize(message));
            var properties = MessageExpiration(Model);

            Model.BasicPublish(exchange: "", routingKey: topic, basicProperties: properties, body: body);
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
                return null;
            }
        }

        private IBasicProperties MessageExpiration(IModel model)
        {
            var props = model.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 1;
            //props.Expiration = "100000";
            return props;
        }
    }
}
