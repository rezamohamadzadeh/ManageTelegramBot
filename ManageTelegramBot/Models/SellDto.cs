using System;

namespace ManageTelegramBot.Models
{
    public class SellDto
    {
        public int Id { get; set; }
        public string AffiliateCode { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Adress { get; set; }
        public string Adress2 { get; set; }
        public string Phone { get; set; }
        public string full_number { get; set; }
        public string FromUrl { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string ZipCode { get; set; }
        public double Price { get; set; }
        public string ProductName { get; set; }
        public string TransActionId { get; set; }

        public DiliveryStatus DiliveryStatus { get; set; }
        public PayStatus PayStatus { get; set; }
        public DateTime CreateAt { get; set; }

    }
    public enum PayStatus
    {
        Registered = 0,
        MonthPaid = 1,
        YaerPaid = 2,
        SixMonth = 3,
        PaidByPayPal = 4
    }
    public enum DiliveryStatus
    {
        undelivered = 0,
        delivered = 1
    }
}
