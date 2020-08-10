using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace ManageTelegramBot.TelegramBot
{
    public interface IManageBotService
    {
        Task EchoAsync(Update update);
    }
}
