using AuthServiceLibrary.Domain.Entities;
using AuthServiceLibrary.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using MongoDB.Driver;

namespace AccountServiceApi.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IUserRepository _rep;

        public AccountController
            (IUserRepository rep)
        {
            _rep = rep;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<User>> GetUserByIdAsync(string userId)
        {
            try
            {
                var result = await _rep.GetUserByIdAsync(userId);
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [HttpPost("balance")]
        public async Task<IActionResult> BalanceRefill([FromQuery] int amount)
        {
            try
            {
                await _rep.BalanceRefill(amount);
                return Ok($"Ваш счет пополнен на {amount}₽!");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetMyBalanceAsync()
        {
            try
            {
                var balance = await _rep.GetMyBalanceAsync();
                return Ok($"Ваш баланс: {balance}₽.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Ошибка: {ex}");
            }
        }


        [Authorize(Roles = "Root")]
        [HttpGet("ping")]
        public IActionResult RootCheck()
        {
            return Ok("PONG!");
        }
    }
}
