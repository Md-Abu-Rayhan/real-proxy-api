using System;

namespace real_proxy_api.Models
{
    public class CryptoPayment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string? TraceId { get; set; } // MixPay internal trace ID
        
        // Transaction Details
        public decimal Amount { get; set; } // quoteAmount
        public string QuoteAssetId { get; set; } = "usd";
        public string? PayeeId { get; set; }
        public string? SettlementAssetId { get; set; }
        
        // User Payment Details (Populated after successful payment)
        public string? PaymentAssetId { get; set; } // The actual crypto used (e.g. BTC)
        public string? PaymentAmount { get; set; } // The actual crypto amount paid
        
        // Status & Links
        public string Status { get; set; } = "Pending";
        public string? PaymentCode { get; set; }
        public string? PaymentUrl { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
