using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQReceiver : IMQReceiver
    {
        public void Ack()
        {
            Model.BasicAck(DeliveryTag, false);
            DeliveryTag = 0;
        }
        private IModel _model;

        public MQReceiver(IServiceProvider serviceProvider, ILogger<MQReceiver> logger)
        {
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        public IServiceProvider ServiceProvider { get; }

        public ILogger<MQReceiver> Logger { get; }

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

        public Action Unsubscribe { get; private set; } = () => { };
        public bool IsStarted { get; private set; } = false;
        public Channel<(string, ulong)> InnerQueue { get; private set; }
        public ulong DeliveryTag { get; private set; }

        public async Task<object> ReceiveAsync(TimeSpan timeout) =>
            await ReceiveAsync<object>(timeout);
        public async Task<T> ReceiveAsync<T>(TimeSpan timeout)
        {
            if (DeliveryTag != 0)
            {
                throw new Exception("Last Message is not Ack or Nack.");
            }

            using (var cts = new CancellationTokenSource())
            {
                try
                {
                    cts.CancelAfter(timeout);
                    (var rawMessage, var deliveryTag) = await InnerQueue.Reader.ReadAsync(cts.Token);

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
            }
            return default;
        }

        public void Nack()
        {
            Model.BasicNack(DeliveryTag, false, true);
            DeliveryTag = 0;
        }

        /// <summary>
        /// safety reenterenc
        /// </summary>
        /// <param name="topic"></param>
        public void Start(string topic)
        {
            if (IsStarted == true) return;

            InnerQueue = Channel.CreateUnbounded<(string, ulong)>();
            var consumer = new EventingBasicConsumer(Model);
            consumer.Received += Consumer_Received;
            Unsubscribe = () => consumer.Received -= Consumer_Received;

            Model.BasicConsume(topic, false, consumer);
            IsStarted = true;

            async void Consumer_Received(object sender, BasicDeliverEventArgs e)
            {
                try
                {
                    //.Net5 전환시 ToArray를 Span으로 수정할 것
                    var rawMessage = Encoding.UTF8.GetString(e.Body.ToArray());
                    await InnerQueue.Writer.WriteAsync((rawMessage, e.DeliveryTag)).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    InnerQueue.Writer.Complete(ex);
                }
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
                    Unsubscribe();
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
