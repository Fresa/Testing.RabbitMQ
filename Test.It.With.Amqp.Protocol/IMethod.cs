﻿namespace Test.It.With.Amqp.Protocol
{
    public interface IMethod
    {
        int ProtocolClassId { get; }
        int ProtocolMethodId { get; }

        void ReadFrom(AmqpReader reader);
        void WriteTo(AmqpWriter writer);
    }
}