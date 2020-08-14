using Microsoft.Extensions.Configuration;
using Telegram.Bot;

namespace ManageTelegramBot.TelegramBot
{
    /// <summary>
    /// Config TelegramBot with BotToken in appsettings.json
    /// </summary>
    public class BotService : IBotService
    {
        private readonly IConfiguration _config;

        public BotService(IConfiguration config)
        {
            _config = config;

            Client = new TelegramBotClient(_config["BotToken"]);
        }

        public TelegramBotClient Client { get; }
    }
}
