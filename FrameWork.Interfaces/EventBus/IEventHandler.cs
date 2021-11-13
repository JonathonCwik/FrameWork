namespace FrameWork.Interfaces.EventBus;

public interface IEventHandler<in TEvent> where TEvent : class
{
    Task<EventHandleResult> Handle(TEvent @event);
}

public enum EventHandleResult {
    Success,
    ExpectedFailure
}


