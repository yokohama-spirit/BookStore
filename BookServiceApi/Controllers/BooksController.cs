using BookServiceLibrary.Application.Requests;
using BookServiceLibrary.Domain.Interfaces;
using DnsClient.Protocol;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static MongoDB.Driver.WriteConcern;
using static System.Net.Mime.MediaTypeNames;

namespace BookServiceApi.Controllers
{
    
    [Route("api/books")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IBooksRepository _booksRep;
        private readonly IBookSearchService _service;
        private readonly IRecoService _reco;
        public BooksController
            (IMediator mediator,
            IBooksRepository booksRep,
            IBookSearchService service,
            IRecoService reco)
        {
            _mediator = mediator;
            _booksRep = booksRep;
            _service = service;
            _reco = reco;
        }
/*
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
░░░░░░░░░░▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄░░░░░░░░░
░░░░░░░░▄▀░░░░░░░░░░░░▄░░░░░░░▀▄░░░░░░░
░░░░░░░░█░░▄░░░░▄░░░░░░░░░░░░░░█░░░░░░░
░░░░░░░░█░░░░░░░░░░░░▄█▄▄░░▄░░░█░▄▄▄░░░
░▄▄▄▄▄░░█░░░░░░▀░░░░▀█░░▀▄░░░░░█▀▀░██░░
░██▄▀██▄█░░░▄░░░░░░░██░░░░▀▀▀▀▀░░░░██░░
░░▀██▄▀██░░░░░░░░▀░██▀░░░░░░░░░░░░░▀██░                  B00KS
░░░░▀████░▀░░░░▄░░░██░░░▄█░░░░▄░▄█░░██░
░░░░░░░▀█░░░░▄░░░░░██░░░░▄░░░▄░░▄░░░██░
░░░░░░░▄█▄░░░░░░░░░░░▀▄░░▀▀▀▀▀▀▀▀░░▄▀░░
░░░░░░█▀▀█████████▀▀▀▀████████████▀░░░░
░░░░░░████▀░░███▀░░░░░░▀███░░▀██▀░░░░░░
░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
*/


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

        [HttpPost("reco/{id}")]
        public async Task<IActionResult> PutReco(string id, [FromBody] string? text = null)
        {
            try
            {
                await _reco.PutRecommended(id, text);
                return Ok("Оценка поставлена!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [HttpPost("unreco/{id}")]
        public async Task<IActionResult> PutUnreco(string id, [FromBody] string? text = null)
        {
            try
            {
                await _reco.PutUnrecommended(id, text);
                return Ok("Оценка поставлена!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [HttpGet("checkr")]
        public async Task<IActionResult> GetRecoById()
        {
            var result = await _reco.GetFullReco();
            return Ok(result);
        }

        [HttpGet("checku")]
        public async Task<IActionResult> GetUnrecoById()
        {
            var result = await _reco.GetFullUnreco();
            return Ok(result);
        }

        [HttpPatch("disc")]
        public async Task<IActionResult> SetDiscount
            ([FromQuery] string b, 
            [FromQuery] int p)
        {
            try
            {
                await _booksRep.SetDiscount(b, p);
                return Ok($"Успешно назначена скидка в {p}%!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }


        [HttpDelete("del")]
        public async Task<ActionResult<string>> RemoveMyProduct([FromQuery] string p)
        {
            try
            {
                await _booksRep.RemoveProductAsync(p);
                return Ok("Продукт успешно удален!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [HttpGet("hc/{productId}")]
        public async Task<ActionResult<string>> HealthCheck(string productId)
        {
            try
            {
                var result = await _booksRep.isExists(productId);
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }
    }
}

/*
11111111111111111111111111111
11111111111111¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶
1111111111¶¶¶¶¶111_________________¶¶
11111111¶¶1_______1111______111_____¶¶
111111¶¶______11_________11111111____¶1
111111¶____11___1_____11______1111___1¶
11111¶1___1_____1_____1_________1_____¶1
11111¶__________________1¶¶¶¶1________1¶
1111¶¶_____¶¶¶¶1______1¶¶_¶¶¶¶¶1_______¶¶
111¶¶_1_1_¶¶¶¶¶¶¶_1___¶__1¶¶¶¶¶¶111____1¶¶
111¶_1________11¶¶1___¶¶¶1__1_____1¶¶¶1__1¶
11¶1__1¶¶1______11_____1____¶¶__1¶¶1__¶¶__1¶
11¶1__111¶¶¶¶___¶1___________1¶¶1___¶__¶1__¶
11¶1____1_11___¶¶_____1¶1_________¶¶¶1__¶__¶1
111¶_1__¶____1¶¶______11¶1_____1¶¶1_¶¶¶1¶__¶1
111¶1__¶¶___11¶¶____¶¶¶_¶___1¶¶¶1___¶__¶___¶1
111¶¶__¶¶¶1_____¶¶1_____11¶¶¶1_¶__1¶¶_____¶11
1111¶__¶¶1¶¶¶1___¶___1¶¶¶¶1____¶¶¶¶¶_____¶¶11
1111¶__¶_1__¶¶¶¶¶¶¶¶¶11__¶__1¶¶¶1_¶_____¶¶111
1111¶1_¶¶¶__1___¶___1____¶¶¶¶¶1¶_¶¶____¶¶1111
1111¶1_¶¶¶¶¶¶¶1¶¶11¶¶1¶¶¶¶¶1___1¶¶_____¶11111
1111¶1_¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶1_¶____¶¶_____¶¶11111
1111¶1_¶¶¶¶¶¶¶¶¶¶¶¶1¶1____¶__1¶¶______¶111111
1111¶__1¶¶_¶_¶__¶___11____1¶¶¶______1¶1111111
1111¶___¶¶1¶_11_11__1¶__1¶¶¶1___11_1¶11111111
1111¶_____¶¶¶¶¶¶¶¶¶¶¶¶¶¶¶1___11111¶¶111111111
1111¶__________11111_______111_1¶¶11111111111
1111¶_1__11____________1111__1¶¶1111111111111
1111¶__11__1________1111___1¶¶111111111111111
1111¶___1111_____________1¶¶11111111111111111
1111¶¶_______________11¶¶¶1111111111111111111
11111¶¶__________1¶¶¶¶¶1111111111111111111111
1111111¶¶¶¶¶¶¶¶¶¶¶111111111111111111111111111
111111111111111111111111111111111111111111111
111111111111111111111111111111111111111111111
*/