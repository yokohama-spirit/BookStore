using AuthServiceLibrary.Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookServiceLibrary.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using AuthServiceLibrary.Domain.Entities;
using Elastic.Clients.Elasticsearch;
using AuthServiceLibrary.Domain.Interfaces;

namespace BookServiceLibrary.Application.Services
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly IMongoCollection<Book> _books;
        private readonly string _indexName;

        public RabbitMQConsumerService
            (IServiceProvider serviceProvider,
            ILogger<RabbitMQConsumerService> logger,
            IMongoDBService mongoDBService,
            ElasticsearchClient elasticClient)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _books = mongoDBService.GetCollection<Book>("Books");
            _elasticClient = elasticClient;
            _indexName = "books-index";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare("remove_book", false, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                using var scope = _serviceProvider.CreateScope();

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var info = JsonSerializer.Deserialize<RemoveBookInfo>(message);


                    var book = await _books
                        .Find(u => u.Id == info.ProductId).FirstOrDefaultAsync() ?? throw new Exception("Книга не найдена.");

                    
                    await _books.DeleteOneAsync(p => p.Id == book.Id);

                    var response = await _elasticClient.DeleteAsync<Book>(book.Id, idx => idx.Index(_indexName));

                    if (!response.IsValidResponse && response.Result != Result.NotFound)
                    {
                        throw new Exception($"Ошибка удаления: {response.DebugInformation}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            };

            channel.BasicConsume("remove_book", true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
