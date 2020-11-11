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

    public class MQSender : IMQSender
    {
        private IModel _model;

        public MQSender(IServiceProvider serviceProvider, ILogger<MQSender> logger)
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
        public ILogger<MQSender> Logger { get; }

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

            string JsonSerialize(object msg)
            {
                try
                {
                    var result = JsonConvert.SerializeObject(msg, Formatting.Indented, new JsonSerializerSettings
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
