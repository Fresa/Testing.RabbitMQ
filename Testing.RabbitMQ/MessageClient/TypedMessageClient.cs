using System;

namespace Test.It.With.RabbitMQ.MessageClient
{
    internal class TypedMessageClient<TMessageReceive> : BaseTypedMessageClient<TMessageReceive>, ITypedMessageClient<TMessageReceive>
    {
        public TypedMessageClient(IMessageClient messageClient, ISerializer serializer) : base(messageClient, serializer)
        {
            base.Received += Received;
            base.Disconnected += Disconnected;
        }

        public new event EventHandler<TMessageReceive> Received;
        public new event EventHandler Disconnected;

        public new void Send<TMessage>(TMessage message)
        {
            base.Send(message);
        }
    }

    internal class TypedMessageClient<TMessageReceive, TMessageSend> : BaseTypedMessageClient<TMessageReceive>, ITypedMessageClient<TMessageReceive, TMessageSend>
    {
        public TypedMessageClient(IMessageClient messageClient, ISerializer serializer) : base(messageClient, serializer)
        {
            base.Received += Received;
            base.Disconnected += Disconnected;
        }

        public new event EventHandler<TMessageReceive> Received;
        public new event EventHandler Disconnected;

        public void Send(TMessageSend message)
        {
            base.Send(message);
        }
    }
}