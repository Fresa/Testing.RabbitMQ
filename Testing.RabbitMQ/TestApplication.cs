﻿using System;
using System.Text;
using RabbitMQ.Client;

namespace Testing.RabbitMQ
{
    class TestApplication
    {
        private readonly IConnectionFactory _connectionFactory;

        public TestApplication(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public void Main()
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare("hello",
                    false,
                    false,
                    false,
                    null);

                string message = "Hello World!";
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish("",
                    "hello",
                    null,
                    body);
                Console.WriteLine(" [x] Sent {0}", message);
            }

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}
