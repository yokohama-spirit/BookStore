﻿using BookServiceLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Domain.Interfaces
{
    public interface IBooksRepository
    {
        Task<IEnumerable<Book>> GetAllAsync();
        Task BuyBook(string bookId,int amount);
        Task<Book> GetBookByIdAsync(string bookId);
        Task SetDiscount(string bookId, int procent);
        Task<string> isExists(string bookId);
        Task RemoveProductAsync(string productId);
    }
}
