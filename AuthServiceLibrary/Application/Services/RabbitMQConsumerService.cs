using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using AuthServiceLibrary.Domain.Entities;
using MongoDB.Driver;
using AuthServiceLibrary.Domain.Interfaces;

namespace AuthServiceLibrary.Application.Services
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private readonly IMongoCollection<User> _users;

        public RabbitMQConsumerService
            (IServiceProvider serviceProvider, 
            ILogger<RabbitMQConsumerService> logger,
            MongoDBService mongoDBService)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _users = mongoDBService.GetCollection<User>("Users");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare("edit_user_balance", false, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                using var scope = _serviceProvider.CreateScope();

                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var info = JsonSerializer.Deserialize<UserOrderInfo>(message);


                    var user = await _users
                        .Find(u => u.Id == info.UserId).FirstOrDefaultAsync() ?? throw new Exception("Пользователь не найден.");

                    user.Balance -= info.SummaryPrice;
                    await _users.ReplaceOneAsync(u => u.Id == info.UserId, user);

                    _logger.LogInformation($"Balance: {user.Balance}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                }
            };

            channel.BasicConsume("edit_user_balance", true, consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
