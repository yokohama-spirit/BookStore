using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Domain.Entities
{
    public class BookBuy
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public required string BookId {  get; set; }
        public required string BuyerId { get; set; }
        public required decimal Summ { get; set; }
        public DateTime BuyDate { get; set; } = DateTime.UtcNow;
    }
}
