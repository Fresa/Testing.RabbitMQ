﻿using System;
using RabbitMQ.Client;
using SimpleInjector;

namespace Test.It.With.RabbitMQ.Tests.TestApplication
{
    public class HeartbeatApplicationSpecification : IApplication
    {
        private SimpleInjectorDependencyResolver _configurer;
        private IConnection _connection;
        private RabbitMqLogEventListener _rabbitmqLogger;

        public void Configure(Action<SimpleInjectorDependencyResolver> reconfigurer)
        {
            _rabbitmqLogger = new RabbitMqLogEventListener();
            var container = new Container();
            container.RegisterSingleton<IConnectionFactory, ConnectionFactory>();

            _configurer = new SimpleInjectorDependencyResolver(container);
            reconfigurer(_configurer);
            _configurer.Verify();
        }

        public void Start(params string[] args)
        {
            var connectionFactory = _configurer.Resolve<IConnectionFactory>();
            _connection = connectionFactory.CreateConnection();
        }

        public void Stop()
        {
            _connection.Dispose();
            _configurer.Dispose();
            _rabbitmqLogger.Dispose();
        }

        public event Action<Exception> OnUnhandledException;
    }
}