using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Application.Requests.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountServiceApi.Controllers
{
    [Route("api/panel")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AdminController(IMediator mediator) => _mediator = mediator;



        //Метод регистрации
        [Authorize(Roles = "Root")]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateAdminRequest command)
        {
            if (!ModelState.IsValid)
            {
                var error = ModelState.Values.SelectMany(e => e.Errors.Select(er => er.ErrorMessage));
                return BadRequest($"Некорректно указаны данные! Ошибка: {error}");
            }
            try
            {
                await _mediator.Send(command);
                return Ok($"Администратор успешно создан!" +
                    $"\nЕго UserName: {command.UserName}" +
                    $"\nПароль: {command.Password}" +
                    $"\nEmail: {command.Email}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("ping")]
        public IActionResult AdminCheck()
        {
            return Ok("ADMIN PONG!");
        }

    }
}
