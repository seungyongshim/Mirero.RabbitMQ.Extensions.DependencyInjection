using System;
using RabbitMQ.Client;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class MQDeclares
    {
        public MQDeclares(Action<IModel> action) => Action = action;

        public Action<IModel> Action { get; }
    }
}
