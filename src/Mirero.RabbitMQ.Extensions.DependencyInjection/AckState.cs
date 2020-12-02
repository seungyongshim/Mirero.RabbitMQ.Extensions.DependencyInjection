using System;

namespace Mirero.RabbitMQ.Extensions.DependencyInjection
{
    public class AckState
    {
        public AckState(ulong deliveryTag) => DeliveryTag = deliveryTag;

        public ulong DeliveryTag { get; private set; }

        public void Reset() => DeliveryTag = 0;

        internal bool IsNotAckSend() => DeliveryTag != 0;

        public static implicit operator AckState(ulong deliveryTag) => new AckState(deliveryTag);
    }
}
