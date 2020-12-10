namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using Mirero.RabbitMQ.Extensions.DependencyInjection;
    using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
    using RabbitMQ.Client;

    public static class AddRabbitMQExtension
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<IModel> declares)
        {
            services.AddHostedService<MQHostedService>();
            services.AddSingleton<MQDeclares>(sp => new MQDeclares(declares));
            services.AddSingleton<MQConnection>();
            services.AddTransient<IMQPublisher, MQPublisher>();
            services.AddTransient<IMQReceiver, MQReceiver>();
            services.AddTransient<IMQRpc, MQRpc>();
            services.AddTransient<IMQChannel, MQChannel>();
            services.AddTransient<IModel>(sp =>
            {
                var conn = sp.GetRequiredService<MQConnection>();
                return conn.CreateModel();
            });
            
            return services;
        }
    }
}
