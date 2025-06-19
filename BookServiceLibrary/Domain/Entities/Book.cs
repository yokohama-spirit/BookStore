using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Domain.Entities
{
    public class Book
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public required string Name { get; set; }
        public required string AuthorName { get; set; }
        public decimal Price { get; set; }
        public int ReleaseDate { get; set; }
        public int Amount { get; set; }

        public int Recommended {  get; set; }
        public int Unrecommended { get; set; }

        public List<Genre> Genres { get; set; } = new List<Genre>();

        public string? TraderId { get; set; }
    }
}
