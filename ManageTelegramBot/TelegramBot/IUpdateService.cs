using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ManageTelegramBot.TelegramBot
{
    public interface IUpdateService
    {
        Task EchoAsync(Update update);
    }
}
