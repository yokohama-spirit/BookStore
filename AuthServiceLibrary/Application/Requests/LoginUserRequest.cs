using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServiceLibrary.Application.Requests
{
    public class LoginUserRequest : IRequest<string>
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
