using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManageTelegramBot.Models
{
    /// <summary>
    /// for store values when receive data from bot
    /// </summary>
    public class BotUserAuth
    {
        public string UserType { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTimeOffset CreateDate { get; set; }
    }

}
