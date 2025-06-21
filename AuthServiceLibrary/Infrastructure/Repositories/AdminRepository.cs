using AuthServiceLibrary.Domain.Entities;
using AuthServiceLibrary.Domain.Interfaces.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static MongoDB.Driver.WriteConcern;
using AuthServiceLibrary.Application.Services;
using MongoDB.Driver;
using RabbitMQ.Client;
using AuthServiceLibrary.Domain.Interfaces;
using BookServiceLibrary.Infrastructure.Data.Roles;

namespace AuthServiceLibrary.Infrastructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly UserSupportForUsers _support;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnection _rabbitMqConnection;
        private readonly IMongoCollection<User> _users;

        public AdminRepository
            (IMongoDBService mongoDBService,
            UserSupportForUsers support,
            IHttpClientFactory httpClientFactory,
            IConnection rabbitMqConnection)
        {
            _support = support;
            _httpClientFactory = httpClientFactory;
            _rabbitMqConnection = rabbitMqConnection;
            _users = mongoDBService.GetCollection<User>("Users");
        }


        public async Task RemoveProduct(string productId)
        {

            var httpClient = _httpClientFactory.CreateClient();
            var serviceUrl = "https://localhost:3002";
            try
            {
                var response = await httpClient.GetStringAsync(
                                    $"{serviceUrl}/api/books/hc/{productId}") ?? throw new HttpRequestException("Продукта не существует.");


                if (response == "N")
                {
                    throw new Exception("Некорректно введен идентификатор товара.");
                }
                else
                {
                    var info = new RemoveBookInfo
                    {
                        ProductId = productId
                    };

                    using var channel = _rabbitMqConnection.CreateModel();
                    channel.QueueDeclare(queue: "remove_book",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    var message = JsonSerializer.Serialize(info);
                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "remove_book",
                                         basicProperties: null,
                                         body: body);

                }

            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Ошибка HTTP запроса: {ex.Message}");
            }

        }

        public async Task SetAdminRoleAsync(string userId)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Eq(u => u.Role, UserRoles.User)
            );

            var update = Builders<User>.Update.Set(u => u.Role, UserRoles.Admin);

            var result = await _users.UpdateOneAsync(filter, update);


            if (result.MatchedCount == 0)
            {
                throw new Exception("Пользователь уже администратор/данные введены некорректно.");
            }
        }

        public async Task RemoveAdminRoleAsync(string userId)
        {
            var filter = Builders<User>.Filter.And(
                Builders<User>.Filter.Eq(u => u.Id, userId),
                Builders<User>.Filter.Eq(u => u.Role, UserRoles.Admin) 
            );

            var update = Builders<User>.Update.Set(u => u.Role, UserRoles.User);

            var result = await _users.UpdateOneAsync(filter, update);


            if (result.MatchedCount == 0)
            {
                throw new Exception("Пользователь не администратор/данные введены некорректно.");
            }
        }
    }
}
