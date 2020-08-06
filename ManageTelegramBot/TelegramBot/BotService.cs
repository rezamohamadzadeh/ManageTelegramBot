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
            //// use proxy if configured in appsettings.*.json
            //Client = string.IsNullOrEmpty(_config.Socks5Host)
            //    ? new TelegramBotClient(_config.BotToken)
            //    : new TelegramBotClient(
            //        _config.BotToken,
            //        new HttpToSocks5Proxy(_config.Socks5Host, _config.Socks5Port));

            Client = new TelegramBotClient(_config["BotToken"]);
        }

        public TelegramBotClient Client { get; }
    }
}
