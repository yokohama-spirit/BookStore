using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Domain.Entities;
using AuthServiceLibrary.Domain.Interfaces;
using AutoMapper;
using MediatR;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Application.Services
{
    public class LoginUserRequestHandle : IRequestHandler<LoginUserRequest, string>
    {
        private readonly IMongoCollection<User> _users;
        private readonly IJwtService _service;

        public LoginUserRequestHandle
            (IMongoDBService mongoDBService,
            IJwtService service)
        {
            _users = mongoDBService.GetCollection<User>("Users");
            _service = service;
        }

        public async Task<string> Handle(LoginUserRequest request, CancellationToken cancellationToken)
        {
            var user = await _users.Find(u => u.UserName == request.UserName).FirstOrDefaultAsync();

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                throw new UnauthorizedAccessException("Некорректно введены данные.");

            var token = await _service.GenerateJwtTokenAsync(user);
            return token;
        }
    }
}
