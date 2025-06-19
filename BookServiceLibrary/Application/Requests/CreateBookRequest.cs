using BookServiceLibrary.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Application.Requests
{
    public class CreateBookRequest : IRequest
    {
        public required string Name { get; set; }
        public required string AuthorName { get; set; }
        public required decimal Price { get; set; }
        public required int ReleaseDate { get; set; }
        public required int Amount { get; set; }

        public List<Genre> Genres { get; set; }

    }
}
