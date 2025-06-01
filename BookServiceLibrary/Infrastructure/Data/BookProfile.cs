using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Domain.Entities;
using AutoMapper;
using BookServiceLibrary.Application.Requests;
using BookServiceLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Infrastructure.Data
{
    public class BookProfile : Profile
    {
        public BookProfile()
        {
            CreateMap<CreateBookRequest, Book>();
        }
    }
}
