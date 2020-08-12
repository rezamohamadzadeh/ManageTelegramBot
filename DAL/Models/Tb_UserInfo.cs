using System.Collections.Generic;

namespace DAL.Models
{
    public class Tb_UserInfo : BaseEntity
    {
        public Tb_UserInfo()
        {
            LoginState = false;
        }
        public long ChatId { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string UserType { get; set; }

        public bool LoginState { get; set; }

        public virtual List<Tb_UserActivities> Tb_UserActivities { get; set; }
    }
}
