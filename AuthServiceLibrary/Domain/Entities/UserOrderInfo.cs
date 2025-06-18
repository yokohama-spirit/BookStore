using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Domain.Entities
{
    public class UserOrderInfo
    {
        public required decimal SummaryPrice { get; set; }
        public required string UserId { get; set; }
    }
}
