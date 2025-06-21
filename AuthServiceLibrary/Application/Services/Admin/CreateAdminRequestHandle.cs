using AuthServiceLibrary.Application.Requests.Admin;
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

namespace AuthServiceLibrary.Application.Services.Admin
{
    public class CreateAdminRequestHandle : IRequestHandler<CreateAdminRequest>
    {
        private readonly IMongoCollection<User> _users;
        private readonly IMapper _mapper;

        public CreateAdminRequestHandle
            (IMongoDBService mongoDBService,
            IMapper mapper)
        {
            _users = mongoDBService.GetCollection<User>("Users");
            _mapper = mapper;
        }

        public async Task Handle(CreateAdminRequest request, CancellationToken cancellationToken)
        {
            var user = await _users.Find(u => u.Email == request.Email || u.UserName == request.UserName).FirstOrDefaultAsync();
            if (user != null)
            {
                throw new Exception("Такие данные уже занятыми другим пользователем.");
            }

            var newAdmin = _mapper.Map<User>(request);

            newAdmin.Role = UserRoles.Admin;
            newAdmin.Password = BCrypt.Net.BCrypt.HashPassword(newAdmin.Password);

            await _users.InsertOneAsync(newAdmin);
        }
    }
}
