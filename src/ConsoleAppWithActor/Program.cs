using Akka;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConsoleAppWithActor
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    var actorSystem = Akka.Actor.ActorSystem.Create("actorSystem");

                    services.AddAkka(actorSystem, new[]
                    {
                        "ConsoleAppWithActor",
                    });
                    services.AddRabbitMQ(model =>
                    {
                        model.QueueDelete("rmq.test.akka.publisher", false, false);
                        model.QueueDeclare("rmq.test.akka.publisher", false, false, false, null);
                    });
                })
                .Build();
        }
    }
}
