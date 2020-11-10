namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    using System;
    using global::RabbitMQ.Client;

    public class MQDeclares
    {
        public MQDeclares(Action<IModel> action) => Action = action;

        public Action<IModel> Action { get; }
    }
}
