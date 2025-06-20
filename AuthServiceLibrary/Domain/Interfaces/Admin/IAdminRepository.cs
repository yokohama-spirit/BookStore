using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Domain.Interfaces.Admin
{
    public interface IAdminRepository
    {
        Task RemoveProduct(string productId);
    }
}
