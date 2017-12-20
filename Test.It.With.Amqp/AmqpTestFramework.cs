using System;
using System.Collections.Generic;
using Log.It;
using Test.It.With.Amqp;
using Test.It.With.Amqp.Expectations;
using Test.It.With.Amqp.Extensions;
using Test.It.With.Amqp.MessageClient;
using Test.It.With.Amqp.MessageHandlers;
using Test.It.With.Amqp.MessageRouters;
using Test.It.With.Amqp.Messages;
using Test.It.With.Amqp.NetworkClient;
using Test.It.With.Amqp.Protocol;
using Test.It.With.Amqp.Protocol.Extensions;
using Test.It.With.Amqp.Subscriptions;
using Frame = Test.It.With.Amqp.Protocol.Frame;

namespace Test.It.With.Amqp
{
    public class AmqpTestFramework : IDisposable
    {
        private readonly ILogger _logger = LogFactory.Create<AmqpTestFramework>();
        private readonly ITypedMessageClient<Frame, Frame> _frameClient;
        private readonly IPublishProtocolHeader _protocolHeaderPublisher;
        private readonly IPublishMethod _methodFramePublisher;
        private readonly IPublish<ContentHeaderFrame> _contentHeaderFramePublisher;
        private readonly IPublish<ContentBodyFrame> _contentBodyFramePublisher;
        private readonly IPublishHeartbeat _heartbeatFramePublisher;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private readonly IExpectationStateMachine _expectationStateMachine;
        private readonly List<Type> _subscribedMethods = new List<Type>();

        public AmqpTestFramework(ProtocolVersion protocolVersion)
        {
            var protocolResolver = new ProtocolResolver(protocolVersion);
            _expectationStateMachine = protocolResolver.ExpectationStateMachine;
            var protocol = protocolResolver.Protocol;

            var networkClientFactory = new InternalRoutedNetworkClientFactory(out var serverNetworkClient);
            networkClientFactory.OnException += exception =>
            {
                // todo: Update base class (it sends Fatal on Error)
                _logger.Error(exception, "Test framework error.");
            };
            ConnectionFactory = networkClientFactory;
            _disposables.Add(networkClientFactory);

            var protocolHeaderHandler = new ProtocolHeaderHandler();
            var methodFrameHandler = new MethodFrameHandler();
            var contentHeaderFrameHandler = new ContentHeaderFrameHandler();
            var contentBodyFrameHandler = new ContentBodyFrameHandler();
            var heartbeatFrameHandler = new HeartbeatFrameHandler();

            var heartbeatFrameRouter = new HeartbeatFrameRouter(null, protocol, heartbeatFrameHandler);
            var contentBodyFrameRouter = new ContentBodyFrameRouter(heartbeatFrameRouter, protocol, contentBodyFrameHandler);
            var contentHeaderFrameRouter = new ContentHeaderFrameRouter(contentBodyFrameRouter, protocol, contentHeaderFrameHandler);
            var methodFrameRouter = new MethodFrameRouter(contentHeaderFrameRouter, protocol, methodFrameHandler);

            var protocolHeaderClient = new ProtocolHeaderClient(serverNetworkClient, protocol);
            protocolHeaderClient.Received += protocolHeaderHandler.Handle;
            var frameClient = new FrameClient(protocolHeaderClient);
            frameClient.Received += methodFrameRouter.Handle;
            _frameClient = frameClient;

            _protocolHeaderPublisher = protocolHeaderHandler;
            _methodFramePublisher = methodFrameHandler;
            _contentHeaderFramePublisher = contentHeaderFrameHandler;
            _contentBodyFramePublisher = contentBodyFrameHandler;
            _heartbeatFramePublisher = heartbeatFrameHandler;
        }

        private void AssertNoDuplicateSubscriptions<TMethod>()
        {
            if (_subscribedMethods.Contains(typeof(TMethod)))
            {
                throw new InvalidOperationException($"There is already a subscription on {typeof(TMethod).GetPrettyFullName()}. There can only be one subscription per method type.");
            }

            _subscribedMethods.Add(typeof(TMethod));
        }

        public INetworkClientFactory ConnectionFactory { get; }

        public void Send<TMessage>(MethodFrame<TMessage> frame) where TMessage : class, IServerMethod
        {
            _logger.Debug($"Sending method {typeof(TMessage).GetPrettyFullName()} on channel {frame.Channel}. {frame.Method.Serialize()}");
            _frameClient.Send(new Frame(Constants.FrameMethod, frame.Channel, frame.Method));
        }

        public void On<TClientMethod>(Action<MethodFrame<TClientMethod>> messageHandler)
            where TClientMethod : class, IClientMethod
        {
            AssertNoDuplicateSubscriptions<TClientMethod>();

            var methodSubscription = _methodFramePublisher.Subscribe<TClientMethod>((frame) =>
            {
                if (_expectationStateMachine.ShouldPass(frame.Channel, frame.Method))
                {
                    _logger.Debug($"Method {typeof(TClientMethod).GetPrettyFullName()} on channel {frame.Channel} was expected.");
                    messageHandler(new MethodFrame<TClientMethod>(
                        frame.Channel,
                        frame.Method));
                }
            });

            _disposables.Add(methodSubscription);

            var contentHeaderSubscription = _contentHeaderFramePublisher.Subscribe(frame =>
            {
                if (_expectationStateMachine.ShouldPass(frame.Channel, frame.ContentHeader,
                    out TClientMethod method))
                {
                    _logger.Debug($"Content header {frame.ContentHeader.GetType().GetPrettyFullName()} for method {typeof(TClientMethod).Name} on channel {frame.Channel} was expected.");
                    messageHandler(new MethodFrame<TClientMethod>(frame.Channel, method));
                }
            });

            _disposables.Add(contentHeaderSubscription);

            var contentBodySubscription = _contentBodyFramePublisher.Subscribe(frame =>
            {
                if (_expectationStateMachine.ShouldPass(frame.Channel, frame.ContentBody, out TClientMethod method))
                {
                    _logger.Debug($"Content body {frame.ContentBody.GetType().GetPrettyFullName()} for method {typeof(TClientMethod).Name} on channel {frame.Channel} was expected.");
                    messageHandler(new MethodFrame<TClientMethod>(frame.Channel, method));
                }
            });

            _disposables.Add(contentBodySubscription);
        }

        public void On<TClientMethod, TServerMethod>(Func<MethodFrame<TClientMethod>, TServerMethod> messageHandler)
            where TClientMethod : class, IClientMethod, INonContentMethod, IRespond<TServerMethod>
            where TServerMethod : class, IServerMethod
        {
            On<TClientMethod>(frame =>
            {
                var response = messageHandler(frame);
                Send(new MethodFrame<TServerMethod>(frame.Channel, response));
            });
        }

        public void On<TProtocolHeader>(Action<ProtocolHeaderFrame<TProtocolHeader>> messageHandler)
            where TProtocolHeader : class, IProtocolHeader
        {
            AssertNoDuplicateSubscriptions<TProtocolHeader>();

            var protocolHeaderSubscription = _protocolHeaderPublisher.Subscribe<TProtocolHeader>(frame =>
            {
                if (_expectationStateMachine.ShouldPass(frame.Channel, (IProtocolHeader)frame.ProtocolHeader))
                {
                    _logger.Debug($"{typeof(TProtocolHeader).GetPrettyFullName()} on channel {frame.Channel} was expected.");
                    messageHandler(frame);
                }
            });

            _disposables.Add(protocolHeaderSubscription);
        }

        public void On<TProtocolHeader>(Func<ProtocolHeaderFrame<TProtocolHeader>, Connection.Start> messageHandler)
            where TProtocolHeader : class, IProtocolHeader
        {
            On<TProtocolHeader>(header =>
           {
               var response = messageHandler(header);
               Send(new MethodFrame<Connection.Start>(0, response));
           });
        }

        public void On<THeartbeat>(Action<HeartbeatFrame<THeartbeat>> messageHandler)
            where THeartbeat : class, IHeartbeat
        {
            AssertNoDuplicateSubscriptions<THeartbeat>();

            var heartbeatSubscription = _heartbeatFramePublisher.Subscribe<THeartbeat>((frame) =>
            {
                if (_expectationStateMachine.ShouldPass(frame.Channel, (IHeartbeat)frame.Heartbeat))
                {
                    _logger.Debug($"{typeof(THeartbeat).GetPrettyFullName()} on channel {frame.Channel} was expected.");
                    messageHandler(frame);
                }
            });

            _disposables.Add(heartbeatSubscription);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}