using System;
using System.Threading.Tasks;
using FrameWork.Interfaces.EventBus;

namespace FrameWork.EventBus.Rabbit.Extension;

public interface IEventBusObserver<TEventBus>
{
    Task OnUpdate(TEventBus eventBus);
}