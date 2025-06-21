using AuthServiceLibrary.Application.Services;
using BookServiceLibrary.Domain.Entities;
using BookServiceLibrary.Domain.Interfaces;
using Elastic.Clients.Elasticsearch;
using MongoDB.Driver;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Application.Services
{
    public class RecoService  : IRecoService
    {
        private readonly IMongoCollection<Book> _books;
        private readonly IMongoCollection<Recommended> _reco;
        private readonly IMongoCollection<Unrecommended> _unreco;
        private readonly IUserSupport _support;
        private readonly ElasticsearchClient _elasticClient;
        private readonly IBooksRepository _rep;

        public RecoService
            (MongoDBService mongoDBService,
            IUserSupport support,
            ElasticsearchClient elasticClient,
            IBooksRepository rep)
        {
            _books = mongoDBService.GetCollection<Book>("Books");
            _reco = mongoDBService.GetCollection<Recommended>("Recommended");
            _unreco = mongoDBService.GetCollection<Unrecommended>("Unrecommended");
            _support = support;
            _elasticClient = elasticClient;
            _rep = rep;
        }
        public async Task PutRecommended(string id, string? text = null)
        {
            var product = await _rep.GetBookByIdAsync(id);
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
            var product = await _rep.GetBookByIdAsync(id);
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

            if (text != null)
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

    }
}
