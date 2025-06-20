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
        private readonly IUserRepository _rep;

        public AdminRepository
            (MongoDBService mongoDBService,
            UserSupportForUsers support,
            IHttpClientFactory httpClientFactory,
            IConnection rabbitMqConnection,
            IUserRepository rep)
        {
            _support = support;
            _httpClientFactory = httpClientFactory;
            _rabbitMqConnection = rabbitMqConnection;
            _rep = rep;
        }

        public async Task RemoveProduct(string productId)
        {
            var myId = await _support.GetCurrentUserId();
            var me = await _rep.GetUserByIdAsync(myId);
            if(me.Role == UserRoles.User)
            {
                throw new Exception("Вы не имеете доступа к данному методу.");
            }

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
    }
}
