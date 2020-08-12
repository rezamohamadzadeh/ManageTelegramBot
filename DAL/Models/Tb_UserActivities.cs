
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Tb_UserActivities : BaseEntity
    {
        public string Message { get; set; }

        [ForeignKey("Tb_UserInfo")]
        public Guid UserInfoId { get; set; }

        public virtual Tb_UserInfo Tb_UserInfo { get; set; }

    }
}
