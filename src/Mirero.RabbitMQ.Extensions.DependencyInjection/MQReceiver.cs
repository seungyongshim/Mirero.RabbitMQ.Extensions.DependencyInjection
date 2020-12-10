namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;
    using Microsoft.Extensions.Logging;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using Newtonsoft.Json;

    public class MQReceiver : IMQReceiver
    {
        public MQReceiver(IServiceProvider serviceProvider, IModel model, ILogger<MQReceiver> logger)
        {
            ServiceProvider = serviceProvider;
            Model = model;
            Logger = logger;
            Model.BasicQos(0, 1, false);
        }

        public IServiceProvider ServiceProvider { get; }
        public ILogger<MQReceiver> Logger { get; }

        public IModel Model { get; set; }

        public Action Unsubscribe { get; private set; } = () => { };

        public bool IsStarted { get; private set; } = false;

        public Channel<(string, ulong)> InnerQueue { get; private set; }

        public async Task<(object, ICommitable)> ReceiveAsync(TimeSpan timeout) =>
            await ReceiveAsync<object>(timeout);

        public async Task<(T, ICommitable)> ReceiveAsync<T>(TimeSpan timeout)
        {
            using (var cts = new CancellationTokenSource(timeout))
            {
                ICommitable commit = null;

                try
                {
                    (var rawMessage, var deliveryTag) = await InnerQueue.Reader.ReadAsync(cts.Token);

                    commit = new Commit(deliveryTag, Ack, Nack);

                    var result = JsonConvert.DeserializeObject<T>(rawMessage, new JsonSerializerSettings
                    {
                        TypeNameHandling = typeof(T).Name == "Object" ? TypeNameHandling.All
                                                                      : TypeNameHandling.None,
                    });

                    return (result, commit);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "");
                    commit?.Nack();
                    throw;
                }
            }
        }

        /// <summary>
        /// http://wish.mirero.co.kr/mirero/project/mls/1.0/h18-mirero-mls10-rd/mls-application/-/issues/1649#note_178824
        /// </summary>
        /// <param name="topic"></param>
        public void Start(string topic)
        {
            if (IsStarted)
            {
                return;
            }

            InnerQueue = Channel.CreateUnbounded<(string, ulong)>();
            var consumer = new AsyncEventingBasicConsumer(Model);
            consumer.Received += Consumer_Received;
            Unsubscribe = () => consumer.Received -= Consumer_Received;

            Model.BasicConsume(topic, false, consumer);
            IsStarted = true;

            async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
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

        private Task Ack(ulong deliveryTag)
        {
            try
            {
                Model.BasicAck(deliveryTag, false);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        private Task Nack(ulong deliveryTag)
        {
            try
            {
                Model.BasicNack(deliveryTag, false, true);
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
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
                    Unsubscribe();
                    Model?.Dispose();
                }

                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
