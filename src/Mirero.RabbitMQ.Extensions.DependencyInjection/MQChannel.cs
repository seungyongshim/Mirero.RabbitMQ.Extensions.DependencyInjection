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
        public void Ack()
        {
            Model.BasicAck(DeliveryTag, false);
            DeliveryTag = 0;
        }

        private IModel _model;

        public MQChannel(IServiceProvider serviceProvider, ILogger<MQChannel> logger)
        {
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        public IModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = ServiceProvider.GetService<IModel>();
                    _model.BasicQos(0, 1, false);
                }

                return _model;
            }
        }
        public IServiceProvider ServiceProvider { get; }
        public ILogger<MQChannel> Logger { get; }
        public ulong DeliveryTag { get; private set; }

        public async Task<object> ReceiveAsync(string topic, CancellationToken ct) =>
            await ReceiveAsync<object>(topic, ct);

        public async Task<T> ReceiveAsync<T>(string topic, CancellationToken ct)
        {
            if (DeliveryTag != 0)
            {
                throw new Exception("Last Message is not Ack or Nack.");
            }

            var ch = Channel.CreateUnbounded<(string, ulong)>();
            var consumer = new EventingBasicConsumer(Model);
            consumer.Received += Consumer_Received;

            try
            {
                Model.BasicConsume(topic, false, consumer);
                
                (var rawMessage, var deliveryTag) = await ch.Reader.ReadAsync(ct);

                var result = JsonConvert.DeserializeObject<T>(rawMessage, new JsonSerializerSettings
                {
                    TypeNameHandling = typeof(T).Name == "Object" ? TypeNameHandling.All
                                                                  : TypeNameHandling.None,
                    

                });

                DeliveryTag = deliveryTag;
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                _model?.Dispose();
                DeliveryTag = 0;
                _model = null;
            }
            finally
            {
                consumer.Received -= Consumer_Received;
            }

            return default;

            async void Consumer_Received(object sender, BasicDeliverEventArgs e)
            {
                try
                {
                    //.Net5 전환시 ToArray를 Span으로 수정할 것
                    var rawMessage = Encoding.UTF8.GetString(e.Body.ToArray());
                    await ch.Writer.WriteAsync((rawMessage, e.DeliveryTag)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    ch.Writer.Complete(ex);
                }
            }
        }

        public void Tell(string topic, object message)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerialize(message));
            var props = Model.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 1;
            props.Expiration = "1800000";

            try
            {
                Model.BasicPublish("", topic, props, body);
            }
            catch(Exception ex)
            {
                Logger.LogError(ex, "");
                _model?.Dispose();
                _model = null;
            }
        }

        private string JsonSerialize(object message)
        {
            try
            {
                var result = JsonConvert.SerializeObject(message, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Objects,
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                });
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                return null;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _model?.Dispose();
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
