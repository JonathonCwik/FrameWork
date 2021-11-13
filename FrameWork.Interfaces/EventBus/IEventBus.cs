namespace FrameWork.Interfaces.EventBus;
using System.Threading.Tasks;

public interface IBusBase
{
    EventBusState GetState();
    Task Start();
    Task Stop();
}

public enum EventBusState {
    NotStarted,
    StartedNotConnected,
    Connected,
    Disconnected
}

public interface IEventPublisher : IBusBase
{
    Task Publish<TEvent>(TEvent @event);
}

public interface IEventSubscriber : IBusBase
{
    Task SubscribeAsync<TEventHandler, TEvent>(string _namespace, TEventHandler eventHandler) where TEventHandler : IEventHandler<TEvent> where TEvent : class;
    void StopSubscribing();
}

public interface IEventBus : IEventPublisher, IEventSubscriber {
    
}
