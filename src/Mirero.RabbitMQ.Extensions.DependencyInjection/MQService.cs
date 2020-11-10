using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    
    public class MQService : IHostedService
    {
        public MQService(MQConnection rabbitMQConnection, ILogger<MQService> logger)
        {
            RabbitMQConnection = rabbitMQConnection;
            Logger = logger;
        }

        public MQConnection RabbitMQConnection { get; }
        public ILogger<MQService> Logger { get; }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            RabbitMQConnection.Connect();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            RabbitMQConnection.Close();
            RabbitMQConnection.Dispose();
            await Task.CompletedTask;
        }
    }
}
