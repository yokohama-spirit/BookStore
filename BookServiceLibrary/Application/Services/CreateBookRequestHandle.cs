using AuthServiceLibrary.Application.Services;
using AutoMapper;
using BookServiceLibrary.Application.Requests;
using BookServiceLibrary.Domain.Entities;
using BookServiceLibrary.Domain.Interfaces;
using MediatR;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Application.Services
{
    public class CreateBookRequestHandle : IRequestHandler<CreateBookRequest>
    {
        private readonly IMongoCollection<Book> _books;
        private readonly IMapper _mapper;
        private readonly IUserSupport _support;
        private readonly IBookSearchService _service;

        public CreateBookRequestHandle
            (MongoDBService mongoDBService,
            IMapper mapper,
            IUserSupport support,
            IBookSearchService service)
        {
            _books = mongoDBService.GetCollection<Book>("Books");
            _mapper = mapper;
            _support = support;
            _service = service;
        }
        public async Task Handle(CreateBookRequest request, CancellationToken cancellationToken)
        {
            var book = _mapper.Map<Book>(request);
            book.TraderId = await _support.GetCurrentUserId();
            await _books.InsertOneAsync(book);
            await _service.IndexBookAsync(book);
        }
    }
}
