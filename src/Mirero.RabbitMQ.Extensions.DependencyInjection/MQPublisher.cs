namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using System;
    using System.Text;
    using global::RabbitMQ.Client;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using Newtonsoft.Json;

    public class MQPublisher : IMQPublisher
    {
        private IModel _model;

        public MQPublisher(IServiceProvider serviceProvider, ILogger<MQPublisher> logger)
        {
            ServiceProvider = serviceProvider;
            Logger = logger;
        }

        public ILogger<MQPublisher> Logger { get; }

        public IModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = ServiceProvider.GetService<IModel>();
                    _model.BasicQos(0, 1, false);
                    var props = Model.CreateBasicProperties();
                        props.ContentType = "application/json";
                        props.DeliveryMode = 1;
                        props.Expiration = "1800000";
                    Props = props;
                }

                return _model;
            }
        }

        public IServiceProvider ServiceProvider { get; }
        public IBasicProperties Props { get; private set; }

        public void Tell(string topic, object message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonSerialize(message));
                Model.BasicPublish("", topic, Props, body);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "");
                _model?.Dispose();
                _model = null;
                throw;
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
                    _model?.Dispose();
                }
                disposedValue = true;
            }
        }

        #endregion IDisposable Support
    }
}
