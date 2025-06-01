using BookServiceLibrary.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookServiceLibrary.Domain.Interfaces
{
    public interface IBookSearchService
    {
        Task IndexBookAsync(Book book);
        Task DeleteBookAsync(string bookId);
        Task<List<Book>> SearchBooksAsync(string query);
    }
}
