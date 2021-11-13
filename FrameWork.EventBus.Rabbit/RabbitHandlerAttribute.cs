namespace FrameWork.EventBus.Rabbit
{
    internal class RabbitHandlerAttribute
    {
        public bool DeliverToAllListeners { get; set; }
        public ushort PrefetchCount { get; set; }
    }
}