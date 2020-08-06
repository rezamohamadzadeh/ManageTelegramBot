using Telegram.Bot;

namespace ManageTelegramBot.TelegramBot
{
    public interface IBotService
    {
        TelegramBotClient Client { get; }
    }
}