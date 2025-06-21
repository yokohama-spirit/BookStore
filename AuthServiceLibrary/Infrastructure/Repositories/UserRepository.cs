using AuthServiceLibrary.Application.Services;
using AuthServiceLibrary.Domain.Entities;
using AuthServiceLibrary.Domain.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;
        private readonly UserSupportForUsers _support;

        public UserRepository
            (MongoDBService mongoDBService,
            UserSupportForUsers support)
        {
            _users = mongoDBService.GetCollection<User>("Users");
            _support = support;
        }

        public async Task BalanceRefill(int amount)
        {
            var userId = await _support.GetCurrentUserId();
            var user = await GetUserByIdAsync(userId);
            user.Balance += amount;
            await _users.ReplaceOneAsync(u => u.Id == userId, user);
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task<decimal> GetMyBalanceAsync()
        {
            var userId = await _support.GetCurrentUserId();
            var user = await GetUserByIdAsync(userId);
            return user.Balance;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            var user = await _users
                .Find(u => u.Id == userId).FirstOrDefaultAsync() ?? throw new Exception("Пользователь не найден.");
            return user;
        }
    }
}
