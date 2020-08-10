
using System;

namespace DAL.Models
{
    public class Tb_UserActivities : BaseEntity
    {
        public string Message { get; set; }


        public Guid UserInfoId { get; set; }
        public virtual Tb_UserInfo Tb_UserInfo { get; set; }

    }
}
