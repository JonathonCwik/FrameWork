using System.Threading.Tasks;
using FrameWork.Interfaces.EventBus;

namespace FrameWork.EventBus.Rabbit.Extension.Extensions
{
    public class BasicRetryObserver<TEventBus> : IEventBusObserver<TEventBus> where TEventBus : IBusBase
    {
        public async Task OnUpdate(TEventBus eventBus)
        {
            if (eventBus.GetState() == EventBusState.Disconnected) {
                await eventBus.Stop();
                await eventBus.Start();
            }
        }
    }
}