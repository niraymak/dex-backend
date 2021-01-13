/*
* Digital Excellence Copyright (C) 2020 Brend Smits
* 
* This program is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation version 3 of the License.
* 
* This program is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty 
* of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
* See the GNU Lesser General Public License for more details.
* 
* You can find a copy of the GNU Lesser General Public License 
* along with this program, in the LICENSE.md file in the root project directory.
* If not, see https://www.gnu.org/licenses/lgpl-3.0.txt
*/

using ElasticSynchronizer.Configuration;
using ElasticSynchronizer.Executors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationSystem.Contracts;
using NotificationSystem.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ElasticSynchronizer.Workers
{
    public class DeleteDocumentsWorker : BackgroundService
    {
        private readonly ILogger<DeleteDocumentsWorker> logger;
        private readonly string subject = "ELASTIC_DELETE_ALL";
        private readonly Config config;

        public DeleteDocumentsWorker(ILogger<DeleteDocumentsWorker> logger, Config config)
        {
            this.logger = logger;
            this.config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            RabbitMQSubscriber subscriber = new RabbitMQSubscriber(new RabbitMQConnectionFactory(config.RabbitMQ.Hostname, config.RabbitMQ.Username, config.RabbitMQ.Password));
            IModel channel = subscriber.SubscribeToSubject(subject);
            RabbitMQListener listener = new RabbitMQListener(channel);
            Console.WriteLine("Hier: ");
            Console.WriteLine(config.Elastic.Hostname);
            Console.WriteLine(config.Elastic.IndexUrl);
            ICallbackService documentsDeleterService = new DocumentDeleter(config);
            EventingBasicConsumer consumer = listener.CreateConsumer(documentsDeleterService);

            listener.StartConsumer(consumer, subject);
        }
    }
}
