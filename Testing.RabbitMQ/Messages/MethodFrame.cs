﻿namespace Test.It.With.RabbitMQ.Messages
{
    public class MethodFrame : BaseFrame
    {
        public MethodFrame(short channel, Amqp.Protocol.IMethod method)
        {
            Channel = channel;
            Method = method;
        }

        public override short Channel { get; }
        public Amqp.Protocol.IMethod Method { get; }
    }

    public class MethodFrame<TMethod> : BaseFrame where TMethod : Amqp.Protocol.IMethod
    {
        public MethodFrame(short channel, TMethod method)
        {
            Channel = channel;
            Method = method;
        }

        public override short Channel { get; }
        public TMethod Method { get; }
    }
}