using BookServiceLibrary.Domain.Entities;
using BookServiceLibrary.Domain.Interfaces;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BookServiceLibrary.Application.Services
{
    public class BookSearchService : IBookSearchService
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly IDistributedCache _redisCache;
        private readonly string _indexName;

        public BookSearchService(
            ElasticsearchClient elasticClient,
            IDistributedCache redisCache)
        {
            _elasticClient = elasticClient;
            _redisCache = redisCache;
            _indexName = "books-index";
        }

        public async Task IndexBookAsync(Book book)
        {
            try
            {
                var response = await _elasticClient.IndexAsync(book, idx => idx.Index(_indexName));

                if (!response.IsValidResponse)
                {
                    Console.WriteLine($"Elasticsearch error: {response.DebugInformation}");
                    Console.WriteLine($"Book data: {JsonSerializer.Serialize(book)}");
                    throw new Exception($"Ошибка индексации: {response.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception during indexing: {ex}");
                throw;
            }
        }

        public async Task DeleteBookAsync(string bookId)
        {
            var response = await _elasticClient.DeleteAsync<Book>(bookId, idx => idx.Index(_indexName));

            if (!response.IsValidResponse && response.Result != Result.NotFound)
            {
                throw new Exception($"Ошибка удаления: {response.DebugInformation}");
            }
        }

        public async Task<List<Book>> SearchBooksAsync(string query)
        {
            var cacheKey = $"books_search:{query}";
            var cachedProducts = await _redisCache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedProducts))
            {
                return JsonSerializer.Deserialize<List<Book>>(cachedProducts) ?? new List<Book>();
            }


            List<Book> products = await SearchByProductDataAsync(query);


            if (products.Count > 0)
            {
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                };

                await _redisCache.SetStringAsync(
                    cacheKey,
                    JsonSerializer.Serialize(products),
                    cacheOptions);
            }

            return products;
        }


        private async Task<List<Book>> SearchByProductDataAsync(string query)
        {
            var response = await _elasticClient.SearchAsync<Book>(s => s
                .Indices(_indexName)
                .Query(q => q
                    .Bool(b => b
                        .Should(

                                      //   SEARCH FOR NAME

            //Prefix Search
            bs => bs.MatchPhrasePrefix(m => m.Field(f => f.Name).Query(query)),
            //Fuzzy Search
            bs => bs.Fuzzy(f => f.Field(f => f.Name).Value(query).Fuzziness(new Fuzziness("AUTO"))),
            //Wildcard
            bs => bs.Wildcard(w => w.Field(f => f.Name).Value($"*{query}*")),


                                      //   SEARCH FOR AUTHOR NAME

            //Prefix Search
            bs => bs.MatchPhrasePrefix(m => m.Field(f => f.AuthorName).Query(query)),
            //Fuzzy Search
            bs => bs.Fuzzy(f => f.Field(f => f.AuthorName).Value(query).Fuzziness(new Fuzziness("AUTO"))),
            //Wildcard
            bs => bs.Wildcard(w => w.Field(f => f.AuthorName).Value($"*{query}*")),


                                     //   SEARCH FOR GENRES

            //Prefix Search
            bs => bs.MatchPhrasePrefix(m => m.Field(f => f.Genres).Query(query)),
            //Fuzzy Search
            bs => bs.Fuzzy(f => f.Field(f => f.Genres).Value(query).Fuzziness(new Fuzziness("AUTO"))),
            //Wildcard
            bs => bs.Wildcard(w => w.Field(f => f.Genres).Value($"*{query}*"))
                        )
                    )
                )
                .Size(150)
            );

            if (!response.IsValidResponse || response.Documents == null)
            {
                return new List<Book>();
            }
            else
            {
                return response.Documents.ToList();
            }
            
        }

    }
}
