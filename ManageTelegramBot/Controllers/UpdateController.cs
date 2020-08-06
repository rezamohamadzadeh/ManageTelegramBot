using System.Threading.Tasks;
using ManageTelegramBot.TelegramBot;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace ManageTelegramBot.Controllers
{
    [Route("api/[controller]")]
    public class UpdateController : Controller
    {
        private readonly IUpdateService _updateService;

        public UpdateController(IUpdateService updateService)
        {
            _updateService = updateService;
        }

        /// <summary>
        /// Post Message From bot
        /// </summary>
        /// <param name="update"></param>
        /// <returns></returns>
        /// 
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            await _updateService.EchoAsync(update);
            return Ok();
        }
    }
}
