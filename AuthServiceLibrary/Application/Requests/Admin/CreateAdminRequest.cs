using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Application.Requests.Admin
{
    public class CreateAdminRequest : IRequest
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
    }
}
