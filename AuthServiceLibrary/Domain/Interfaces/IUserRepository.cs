using AuthServiceLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByIdAsync(string userId);
        Task BalanceRefill(int amount);
        Task<decimal> GetMyBalanceAsync();
    }
}
