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
using System.Net;

namespace BookServiceLibrary.Infrastructure.Repositories
{
    public class BooksRepository : IBooksRepository
    {
        private readonly IMongoCollection<Book> _books;
        private readonly IMongoCollection<BookBuy> _booksbuy;
        private readonly IMongoCollection<Recommended> _reco;
        private readonly IMongoCollection<Unrecommended> _unreco;
        private readonly IUserSupport _support;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnection _rabbitMqConnection;
        private readonly ElasticsearchClient _elasticClient;

        public BooksRepository
            (MongoDBService mongoDBService,
            IUserSupport support,
            IHttpClientFactory httpClientFactory,
            IConnection rabbitMqConnection,
            ElasticsearchClient elasticClient)
        {
            _books = mongoDBService.GetCollection<Book>("Books");
            _booksbuy = mongoDBService.GetCollection<BookBuy>("BooksBuy");
            _reco = mongoDBService.GetCollection<Recommended>("Recommended");
            _unreco = mongoDBService.GetCollection<Unrecommended>("Unrecommended");
            _support = support;
            _httpClientFactory = httpClientFactory;
            _rabbitMqConnection = rabbitMqConnection;
            _elasticClient = elasticClient;
        }

        public async Task<Book> GetBookByIdAsync(string bookId)
        {
            var book = await _books
                .Find(u => u.Id == bookId).FirstOrDefaultAsync() ?? throw new Exception("Книга не найдена.");
            return book;
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

                        var updateResponse = await _elasticClient.UpdateAsync<Book, object>(
                        "books-index", 
                        bookId,  
                        u => u.Doc(new { amount = book.Amount }) 
                        );

                        if (!updateResponse.IsValidResponse)
                        {
                            throw new Exception($"Ошибка обновления: {updateResponse.DebugInformation}");
                        }

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

        public async Task PutRecommended(string id, string? text = null)
        {
            var product = await GetBookByIdAsync(id);
            var userId = await _support.GetCurrentUserId();
            var reco = await _reco
                .Find
                (r => r.UserId == userId && r.ProductId == id)
                .FirstOrDefaultAsync();

            if (reco != null)
            {
                throw new Exception("Нельзя оценить продукт дважды.");
            }

            var unreco = await _unreco
                .Find
                (r => r.UserId == userId && r.ProductId == id)
                .FirstOrDefaultAsync();

            if (unreco != null && product.Unrecommended > 0)
            {
                product.Unrecommended--;
                await _books.ReplaceOneAsync(b => b.Id == id, product);
                await _unreco.DeleteOneAsync(p => p.Id == unreco.Id);
            }


            var rec = new Recommended
            {
                UserId = userId,
                ProductId = id
            };
            if (text != null)
            {
                rec.Text = text;
            }

            await _reco.InsertOneAsync(rec);
            product.Recommended++;

            await _books.ReplaceOneAsync(b => b.Id == id, product);

            var updateResponse = await _elasticClient.UpdateAsync<Book, object>(
            "books-index",
            id,
            u => u.Doc(new { recommended = product.Recommended })
            );

            if (!updateResponse.IsValidResponse)
            {
                throw new Exception($"Ошибка обновления: {updateResponse.DebugInformation}");
            }
        }

        public async Task PutUnrecommended(string id, string? text = null)
        {
            var product = await GetBookByIdAsync(id);
            var userId = await _support.GetCurrentUserId();
            var unreco = await _unreco
                .Find
                (r => r.UserId == userId && r.ProductId == id)
                .FirstOrDefaultAsync();

            if (unreco != null)
            {
                throw new Exception("Нельзя оценить продукт дважды.");
            }

            var reco = await _reco
                .Find
                (r => r.UserId == userId && r.ProductId == id)
                .FirstOrDefaultAsync();

            if (reco != null && product.Recommended > 0)
            {
                product.Recommended--;
                await _reco.DeleteOneAsync(p => p.Id == reco.Id);
            }


            var unr = new Unrecommended
            {
                UserId = userId,
                ProductId = id
            };

            if(text != null)
            {
                unr.Text = text;
            }

            await _unreco.InsertOneAsync(unr);
            product.Unrecommended++;

            await _books.ReplaceOneAsync(b => b.Id == id, product);

            var updateResponse = await _elasticClient.UpdateAsync<Book, object>(
            "books-index",
            id,
            u => u.Doc(new { unrecommended = product.Unrecommended })
            );

            if (!updateResponse.IsValidResponse)
            {
                throw new Exception($"Ошибка обновления: {updateResponse.DebugInformation}");
            }
        }

        public async Task<List<Recommended>> GetFullReco()
        {
            return await _reco.Find(_ => true).ToListAsync();
        }

        public async Task<List<Unrecommended>> GetFullUnreco()
        {
            return await _unreco.Find(_ => true).ToListAsync();
        }

        public async Task SetDiscount(string bookId, int procent)
        {
            var product = await GetBookByIdAsync(bookId);
            var myId = await _support.GetCurrentUserId();
            if(product.TraderId != myId)
            {
                throw new Exception("Нельзя изменить чужой товар!");
            }

            if(procent < 1 || procent >= 100)
            {
                throw new Exception("Некорретно введены данные для скидки.");
            }
            var discount = product.Price / 100 * procent;
            product.Price -= discount;
            await _books.ReplaceOneAsync(b => b.Id == bookId, product);

            var updateResponse = await _elasticClient.UpdateAsync<Book, object>(
            "books-index",
            bookId,
            u => u.Doc(new { price = product.Price })
            );

            if (!updateResponse.IsValidResponse)
            {
                throw new Exception($"Ошибка обновления: {updateResponse.DebugInformation}");
            }
        }
    }
}
