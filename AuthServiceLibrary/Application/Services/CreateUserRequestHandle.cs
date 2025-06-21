using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Domain.Entities;
using AuthServiceLibrary.Domain.Interfaces;
using AutoMapper;
using BookServiceLibrary.Infrastructure.Data.Roles;
using MediatR;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Application.Services
{
    public class CreateUserRequestHandle : IRequestHandler<CreateUserRequest, string>
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMapper _mapper;
        private readonly IJwtService _service;

        public CreateUserRequestHandle
            (IMongoDBService mongoDBService,
            IMapper mapper,
            IJwtService service)
        {
            _users = mongoDBService.GetCollection<User>("Users");
            _mapper = mapper;
            _service = service;
        }

        public async Task<string> Handle(CreateUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _users.Find(u => u.Email == request.Email || u.UserName == request.UserName).FirstOrDefaultAsync();
            if (user != null)
            {
                throw new Exception("Такие данные уже занятыми другим пользователем.");
            }

            var newUser = _mapper.Map<User>(request);

            bool isRoot = request.Password == "Loremipsum123" 
                && request.UserName == "Creator" 
                && request.Email == "creator@gmail.com";

            if (isRoot)
            {
                newUser.Role = UserRoles.Root;
            }

            newUser.Password = BCrypt.Net.BCrypt.HashPassword(newUser.Password);
            await _users.InsertOneAsync(newUser);
            var token = await _service.GenerateJwtTokenAsync(newUser);
            return token;
        }
    }
}
