using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQHostedService : IHostedService
    {
        public MQHostedService(MQConnection rabbitMQConnection, ILogger<MQHostedService> logger)
        {
            MQConnection = rabbitMQConnection;
            Logger = logger;
        }

        public MQConnection MQConnection { get; }
        public ILogger<MQHostedService> Logger { get; }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            MQConnection.Connect();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            MQConnection.Dispose();
            await Task.CompletedTask;
        }
    }
}
