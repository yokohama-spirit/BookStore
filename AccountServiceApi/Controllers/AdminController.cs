using AuthServiceLibrary.Application.Requests;
using AuthServiceLibrary.Application.Requests.Admin;
using AuthServiceLibrary.Domain.Interfaces.Admin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace AccountServiceApi.Controllers
{
    [Route("api/panel")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IAdminRepository _rep;
        public AdminController
            (IMediator mediator,
            IAdminRepository rep)
        {
            _mediator = mediator;
            _rep = rep;
        }

/*       ⢀⣠⣤⣶⣶⣶⣶⣶⣤⣄⡀⠀⠀⠀⠀⠀⠀⠀⠀⠀
⠀⠀⠀⠀⠀⠀⣠⣴⣾⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣄⡀⠀⠀⠀⠀⠀
⠀⠀⠀⣠⣴⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣮⣵⣄⠀⠀⠀
⠀⠀⢾⣻⣿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢿⣿⣿⡀⠀
⠀⠸⣽⣻⠃⣿⡿⠋⣉⠛⣿⣿⣿⣿⣿⣿⣿⣿⣏⡟⠉⡉⢻⣿⡌⣿⣳⡥⠀
⠀⢜⣳⡟⢸⣿⣷⣄⣠⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿⣧⣤⣠⣼⣿⣇⢸⢧⢣⠀                      METHODS FOR ROOT-USER
⠀⠨⢳⠇⣸⣿⣿⢿⣿⣿⣿⣿⡿⠿⠿⠿⢿⣿⣿⣿⣿⣿⣿⣿⣿⠀⡟⢆⠀
⠀⠀⠈⠀⣾⣿⣿⣼⣿⣿⣿⣿⡀⠀⠀⠀⠀⣿⣿⣿⣿⣿⣽⣿⣿⠐⠈⠀⠀
⠀⢀⣀⣼⣷⣭⣛⣯⡝⠿⢿⣛⣋⣤⣤⣀⣉⣛⣻⡿⢟⣵⣟⣯⣶⣿⣄⡀⠀
⣴⣿⣿⣿⣿⣿⣿⣿⣿⣿⣷⣶⣶⣶⣾⣶⣶⣴⣾⣿⣿⣿⣿⣿⣿⢿⣿⣿⣧
⣿⣿⣿⠿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⠿⠿⣿⡿
*/


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


        [Authorize(Roles = "Root")]
        [HttpPost("sa")]
        public async Task<IActionResult> SetAdminRole([FromQuery] string p)
        {
            try
            {
                await _rep.SetAdminRoleAsync(p);
                return Ok("Роль админа успешно назначена!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [Authorize(Roles = "Root")]
        [HttpPost("ra")]
        public async Task<IActionResult> RemAdminRole([FromQuery] string p)
        {
            try
            {
                await _rep.RemoveAdminRoleAsync(p);
                return Ok("Роль админа успешно снята.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

                                           // AND FOR ADMINS

        [Authorize(Policy = "AdminOrRoot")]
        [HttpDelete("del")]
        public async Task<IActionResult> RemoveProduct([FromQuery] string p)
        {
            try
            {
                await _rep.RemoveProduct(p);
                return Ok("Товар успешно снят с прилавка!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

    }
}
