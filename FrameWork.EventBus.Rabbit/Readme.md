# FrameWork.EventBus.Rabbit

## Purpose

FrameWork.EventBus.Rabbit is a way to quickly setup an event bus in and across applications.

## Usage

### Set Up Connection Options
```
var rabbitOptions = new Action<ConnectionOptions>((opt) => {
    opt.URIs = new List<Uri>() {
        new Uri("amqp://guest:guest@localhost:5672/")
        // can provide multiple URIs for High Availability
    };
    opt.AutomaticReconnect = true;
    opt.HeartbeatInterval = 30;
});
```

### Create Publisher If Needed

Currently the Protobuf Serializer is the only IByteSerializer implementation (JSON is in the works) unless you make your own. You will need to install the `FrameWork.Serialization.Protobuf` nuget package to use it.

```
var publisher = new RabbitEventPublisher("pubnamespace", rabbitOptions, 
    new ProtobufSerializer());
await publisher.Start();
```

### Create Subscriber If Needed

You will want to keep your Serializer the same across your domain so if you choose the Protobuf Serializer you will need to stick with that.

```
var subscriber = new RabbitEventSubscriber("subnamespace", 
    rabbitOptions, 
    new ProtobufSerializer());
subscriber.Start();
```

### Subscribe to Event

If your app has multiple namespaces (domains), the suggested pattern is to have the source domain package and distribute the events for the other namespaces.

An event is published in the namespace of the publisher, so when subscribing, supply the publisher namespace.

```
// Attributes needed if using the Protobuf Library
[ProtoContract]
public class TestEvent {
    [ProtoMember(1)]
    public Guid Id {get;set;}
}

private class TestSubscriber : IEventHandler<TestEvent>
{
    public Task<EventHandleResult> Handle(TestEvent @event)
    {
        // Do Something
    }
}

...

await subscriber.SubscribeAsync<TestSubscriber, TestEvent>("pubnamespace", eventHandler);
```

### Publish Event

A published event will always publish to the publisher's namespace.

```
await publisher.Publish(new TestEvent() {
    Id = Guid.NewGuid()
});
```

## Extending

The current state of extending is the ability to observe state change and change the bus as a result of the state change. A common use case would be to refresh an internal list of available nodes if the connection disconnects. Provided is an example of a simple retry on disconnect.

[BasicRetryObserver](https://github.com/JonathonCwik/FrameWork/blob/main/FrameWork.EventBus.Rabbit/Extension/Extensions/BasicRetryObserver.cs)

