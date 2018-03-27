﻿using Test.It.With.Amqp.Protocol;

namespace Test.It.With.RabbitMQ
{
    internal class RabbitMq091ProtocolGeneratorDecorator : IProtocol
    {
        private readonly IProtocol _protocol;

        public RabbitMq091ProtocolGeneratorDecorator(IProtocol protocol)
        {
            _protocol = protocol;
        }

        public IVersion Version => _protocol.Version;
        public IProtocolHeader GetProtocolHeader(IAmqpReader reader)
        {
            return _protocol.GetProtocolHeader(reader);
        }

        public IMethod GetMethod(IAmqpReader reader)
        {
            if (TryGetMethod(reader, out var method))
            {
                return method;
            }

            return _protocol.GetMethod(reader);
        }

        private bool TryGetMethod(IAmqpReader reader, out IMethod method)
        {
            throw new System.NotImplementedException();
        }

        //public class ConfirmSelect : RabbitMQ.Client.Impl.MethodBase, IConfirmSelect
        //{
        //    public const int ClassId = 85;
        //    public const int MethodId = 10;

        //    public bool m_nowait;

        //    bool IConfirmSelect.Nowait { get { return m_nowait; } }

        //    public ConfirmSelect() { }
        //    public ConfirmSelect(
        //      bool initNowait)
        //    {
        //        m_nowait = initNowait;
        //    }

        //    public override int ProtocolClassId { get { return 85; } }
        //    public override int ProtocolMethodId { get { return 10; } }
        //    public override string ProtocolMethodName { get { return "confirm.select"; } }
        //    public override bool HasContent { get { return false; } }

        //    public override void ReadArgumentsFrom(RabbitMQ.Client.Impl.MethodArgumentReader reader)
        //    {
        //        m_nowait = reader.ReadBit();
        //    }

        //    public override void WriteArgumentsTo(RabbitMQ.Client.Impl.MethodArgumentWriter writer)
        //    {
        //        writer.WriteBit(m_nowait);
        //    }

        //    public override void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        //    {
        //        sb.Append("(");
        //        sb.Append(m_nowait);
        //        sb.Append(")");
        //    }
        //}
        ///// <summary>Autogenerated type. Private implementation class - do not use directly.</summary>
        //public class ConfirmSelectOk : RabbitMQ.Client.Impl.MethodBase, IConfirmSelectOk
        //{
        //    public const int ClassId = 85;
        //    public const int MethodId = 11;



        //    public ConfirmSelectOk(
        //)
        //    {
        //    }

        //    public override int ProtocolClassId { get { return 85; } }
        //    public override int ProtocolMethodId { get { return 11; } }
        //    public override string ProtocolMethodName { get { return "confirm.select-ok"; } }
        //    public override bool HasContent { get { return false; } }

        //    public override void ReadArgumentsFrom(RabbitMQ.Client.Impl.MethodArgumentReader reader)
        //    {
        //    }

        //    public override void WriteArgumentsTo(RabbitMQ.Client.Impl.MethodArgumentWriter writer)
        //    {
        //    }

        //    public override void AppendArgumentDebugStringTo(System.Text.StringBuilder sb)
        //    {
        //        sb.Append("(");
        //        sb.Append(")");
        //    }
        //}

        public IContentHeader GetContentHeader(IAmqpReader reader)
        {
            return _protocol.GetContentHeader(reader);
        }

        public IContentBody GetContentBody(IAmqpReader reader)
        {
            return _protocol.GetContentBody(reader);
        }

        public IHeartbeat GetHeartbeat(IAmqpReader reader)
        {
            return _protocol.GetHeartbeat(reader);
        }
    }
}