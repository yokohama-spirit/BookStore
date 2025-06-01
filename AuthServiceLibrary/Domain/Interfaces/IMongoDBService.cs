using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Domain.Interfaces
{
    public interface IMongoDBService
    {
        public IMongoCollection<T> GetCollection<T>(string collectionName);
    }
}
