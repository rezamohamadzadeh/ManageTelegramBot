﻿
namespace Accounting.Models
{
    public class AffiliateReportDto
    {
        public string AffiliateEmail { get; set; }
        public string AffiliateCode { get; set; }
        public int RegisteredCount { get; set; }
        public double SumSell { get; set; }
    }
}
