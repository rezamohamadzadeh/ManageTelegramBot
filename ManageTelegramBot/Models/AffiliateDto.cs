using System;
using System.ComponentModel.DataAnnotations;

namespace ManageTelegramBot.Models
{
    public class JsonResultContent
    {
        public string Message { get; set; }
        public JsonStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }

    }
    public class JsonResultContent<TEntity> : JsonResultContent
    {
        public TEntity Data { get; set; }
    }
    public enum JsonStatusCode
    {
        [Display(Name = "The request was successful")]
        Success,
        [Display(Name = "The request encountered an error")]
        Error,
        [Display(Name = "Submitted parameters are invalid")]
        Warning,
        [Display(Name = "You do not have access to this section")]
        Forbidden,
        [Display(Name = "You are unauthorized")]
        UnAuthorized

    }
    public class AffiliateDto
    {

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string phone { get; set; }
        public string Company { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
        public string CreateAt { get; set; }
        public string Code { get; set; }
        public int Click { get; set; }
        public int comision { get; set; }
    }
}
