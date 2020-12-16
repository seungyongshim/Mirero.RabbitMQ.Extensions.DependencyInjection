using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection.Options
{
    [DataContract]
    public class MQConnectionOptions
    {
        public static readonly string Section = "rabbitmq:connection";
        public string Vhost { get; set; }
        public string Hostname { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        [DataMember(Name = "client-queue-name")]
        public string ClientQueueName {get;set;}
    }
}