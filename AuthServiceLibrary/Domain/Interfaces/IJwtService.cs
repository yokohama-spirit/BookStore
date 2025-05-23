using AuthServiceLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Domain.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateJwtTokenAsync(User user);
    }
}
