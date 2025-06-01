using BookServiceLibrary.Application.Requests;
using BookServiceLibrary.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookServiceApi.Controllers
{
    
    [Route("api/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IBooksRepository _booksRep;
        private readonly IBookSearchService _service;
        public BooksController
            (IMediator mediator,
            IBooksRepository booksRep,
            IBookSearchService service)
        {
            _mediator = mediator;
            _booksRep = booksRep;
            _service = service;
        }



        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookRequest command)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(e => e.Errors.Select(er => er.ErrorMessage));
                return BadRequest($"Некорректно указаны данные! Ошибка: {error}");
            }
            try
            {
                await _mediator.Send(command);
                return Ok("Книга добавлена!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var books = await _booksRep.GetAllAsync();
            return Ok(books);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchBookAsync([FromQuery] string query)
        {
            try
            {
                var result = await _service.SearchBooksAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [HttpPost("buy/{bookId}/{amount}")]
        public async Task<IActionResult> BuyBookAsync(string bookId, int amount)
        {
            try
            {
                await _booksRep.BuyBook(bookId, amount);
                return Ok("Покупка успешно совершена!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

    }
}
