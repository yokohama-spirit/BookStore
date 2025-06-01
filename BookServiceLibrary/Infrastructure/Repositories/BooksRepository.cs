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

namespace BookServiceLibrary.Infrastructure.Repositories
{
    public class BooksRepository : IBooksRepository
    {
        private readonly IMongoCollection<Book> _books;
        private readonly IMongoCollection<BookBuy> _booksbuy;
        private readonly IMongoCollection<User> _users;
        private readonly IUserSupport _support;
        private readonly IHttpClientFactory _httpClientFactory;

        public BooksRepository
            (MongoDBService mongoDBService,
            IUserSupport support,
            IHttpClientFactory httpClientFactory)
        {
            _books = mongoDBService.GetCollection<Book>("Books");
            _booksbuy = mongoDBService.GetCollection<BookBuy>("BooksBuy");
            _users = mongoDBService.GetCollection<User>("Users");
            _support = support;
            _httpClientFactory = httpClientFactory;
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


                        response.Balance -= summaryPrice;
                        await _users.ReplaceOneAsync(u => u.Id == myId, response);
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
