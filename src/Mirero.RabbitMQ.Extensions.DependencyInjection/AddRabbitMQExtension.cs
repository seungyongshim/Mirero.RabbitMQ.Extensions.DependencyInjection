using System;
using Microsoft.Extensions.Configuration;
using Mirero.RabbitMQ.Extensions.DependencyInjection;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Common;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Options;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AddRabbitMQExtension
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<IModel> declares) =>
            services.AddRabbitMQ(null, declares);

        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, Action<IModel> declares)
        {
            if (configuration != null)
            {
                services.AddOptions();
                services.AddOptions<MQConnectionOptions>().Bind(configuration.GetSection(MQConnectionOptions.Section));
            }

            services.AddHostedService<MQHostedService>();
            services.AddSingleton<MQDeclares>(sp => new MQDeclares(declares));
            services.AddSingleton<MQConnection>();
            services.AddTransient<IMQPublisher, MQPublisher>();
            services.AddTransient<IMQReceiver, MQReceiver>();
            services.AddTransient<IMQRpcClient, MQRpcClient>();
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