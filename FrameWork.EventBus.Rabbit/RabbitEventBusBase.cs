using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FrameWork.EventBus.Rabbit.Extension;
using FrameWork.Interfaces;
using FrameWork.Interfaces.EventBus;
using RabbitMQ.Client;

namespace FrameWork.EventBus.Rabbit
{
    public class RabbitEventBusBase<TEventBus> : IBusBase where TEventBus : IBusBase
    {
        private TEventBus eventBus { get; set; }
        public EventBusState State { get; private set; }
        public EventBusState PreviousState { get; private set; }
        private readonly Action<ConnectionOptions> options;
        private List<IEventBusObserver<TEventBus>> observers = new List<IEventBusObserver<TEventBus>>();
        public IByteSerializer Serializer { get; }
        public static IConnection conn {get; private set; }

        public RabbitEventBusBase(Action<ConnectionOptions> options,
                          IByteSerializer serializer)
        {
            this.Serializer = serializer;
            this.options = options;
        }

        protected void SetEventBus(TEventBus eventBus) {
            this.eventBus = eventBus;
        }

        public void Attach(IEventBusObserver<TEventBus> observer) {
            observers.Add(observer);
        }

        public virtual async Task Start()
        {
            conn = await this.GetOrStartConnection(conn);
        }

        private async Task<IConnection> GetOrStartConnection( 
            IConnection connection)
        {
            if (connection == null || !connection.IsOpen)
            {
                connection = await RabbitConnectionFactory.Create(options);
                connection.ConnectionShutdown += async (sender, args) =>
                {
                    if (PreviousState == EventBusState.Connected) {
                        await UpdateState(EventBusState.Disconnected);
                    }
                };

                connection.RecoverySucceeded += async (sender, args) => { 
                    await UpdateState(EventBusState.Connected);
                };
            }

            return connection;
        }

        protected async Task UpdateState(EventBusState newState) {
            PreviousState = State;
            State = newState;
            await this.Notify();
        }

        private async Task Notify() {
            foreach (var o in observers) {
                await o.OnUpdate(eventBus);
            }
        }

        public EventBusState GetState()
        {
            return State;
        }

        public virtual async Task Stop()
        {
            await UpdateState(EventBusState.NotStarted);
            if (conn.IsOpen)
            {
                conn.Close();
            }
        }

        public bool IsConnOpen()
        {
            return conn.IsOpen;
        }
    }
}