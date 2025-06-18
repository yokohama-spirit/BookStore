using AuthServiceLibrary.Application.Services;
using AutoMapper;
using BookServiceLibrary.Domain.Entities;
using BookServiceLibrary.Domain.Interfaces;
using Elastic.Clients.Elasticsearch;
using MongoDB.Driver;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AuthServiceLibrary.Domain.Entities;
using Elastic.Clients.Elasticsearch.Security;
using User = AuthServiceLibrary.Domain.Entities.User;
using RabbitMQ.Client;

namespace BookServiceLibrary.Infrastructure.Repositories
{
    public class BooksRepository : IBooksRepository
    {
        private readonly IMongoCollection<Book> _books;
        private readonly IMongoCollection<BookBuy> _booksbuy;
        private readonly IUserSupport _support;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnection _rabbitMqConnection;

        public BooksRepository
            (MongoDBService mongoDBService,
            IUserSupport support,
            IHttpClientFactory httpClientFactory,
            IConnection rabbitMqConnection)
        {
            _books = mongoDBService.GetCollection<Book>("Books");
            _booksbuy = mongoDBService.GetCollection<BookBuy>("BooksBuy");
            _support = support;
            _httpClientFactory = httpClientFactory;
            _rabbitMqConnection = rabbitMqConnection;
        }

        public async Task BuyBook(string bookId, int amount)
        {
            var book = await _books.Find(p => p.Id == bookId)
                .FirstOrDefaultAsync() ?? throw new Exception("Книга не найдена.");
            if(book.Amount < amount || book.Amount == 0)
            {
                throw new Exception("Некорректное кол-во запрашиваемых книг.");
            }
            else
            {
                var myId = await _support.GetCurrentUserId();
                var httpClient = _httpClientFactory.CreateClient();
                var serviceUrl = "https://localhost:3001";
                try
                {
                    var response = await httpClient.GetFromJsonAsync<User>(
                                        $"{serviceUrl}/api/account/{myId}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new HttpRequestException("Продукта не существует.");


                    var summaryPrice = book.Price * amount;
                    if(summaryPrice > response.Balance)
                    {
                        throw new Exception("Невозможно совершить покупку, превышающую ваш баланс.");
                    }
                    else
                    {
                        var bookBuy = new BookBuy
                        {
                            BookId = bookId,
                            BuyerId = myId,
                            Summ = summaryPrice
                        };
                        await _booksbuy.InsertOneAsync(bookBuy);


                        book.Amount -= amount;
                        await _books.ReplaceOneAsync(b => b.Id == bookId, book);

                        var info = new UserOrderInfo
                        {
                            SummaryPrice = summaryPrice,
                            UserId = myId
                        };

                        using var channel = _rabbitMqConnection.CreateModel();
                        channel.QueueDeclare(queue: "edit_user_balance",
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);

                        var message = JsonSerializer.Serialize(info);
                        var body = Encoding.UTF8.GetBytes(message);

                        channel.BasicPublish(exchange: "",
                                             routingKey: "edit_user_balance",
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

        public async Task<IEnumerable<Book>> GetAllAsync()
        {
            var books = await _books.Find(_ => true).ToListAsync();
            return books;
        }
    }
}
