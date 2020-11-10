using System;
using Mirero.RabbitMQ.Extensions.DependencyInjection;
using Mirero.RabbitMQ.Extensions.DependencyInjection.Abstractions;
using RabbitMQ.Client;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AddRabbitMQExtension
    {
        public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<IModel> declares)
        {
            services.AddHostedService<MQService>();
            services.AddSingleton<MQDeclares>(sp => new MQDeclares(declares));
            services.AddSingleton<MQConnection>();
            services.AddScoped<IModel>(sp =>
            {
                var conn = sp.GetRequiredService<MQConnection>();
                return conn.CreateModel();
            });
            services.AddScoped<IMQChannel, MQChannel>();

            return services;
        }
    }
}
