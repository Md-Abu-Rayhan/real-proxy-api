namespace real_proxy_api.Models
{
    /// <summary>
    /// Main payment transaction record
    /// Stores all payment information and transaction status
    /// </summary>
    public class Payment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        
        // Transaction Identifiers
        public string CustomerOrderId { get; set; } = string.Empty;
        public string MerchantTransactionId { get; set; } = string.Empty;
        public string? EpsTransactionId { get; set; }
        
        // Transaction Details
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BDT";
        public int TransactionTypeId { get; set; } // 1=Web, 2=Android, 3=IOS
        
        // Payment Status
        public string Status { get; set; } = "Pending"; // Pending, Success, Failed, Cancelled, Expired
        public string? PaymentMethod { get; set; } // Will be filled after verification (e.g., OKWallet, bKash, etc.)
        
        // Product Information
        public string ProductName { get; set; } = string.Empty;
        public string? ProductProfile { get; set; }
        public string? ProductCategory { get; set; }
        public int Quantity { get; set; } = 1;
        
        // Customer Information (for verification and reference)
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerState { get; set; }
        public string? CustomerPostcode { get; set; }
        public string? CustomerCountry { get; set; }
        
        // URLs
        public string? SuccessUrl { get; set; }
        public string? FailUrl { get; set; }
        public string? CancelUrl { get; set; }
        
        // Security & Verification
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? VerificationHash { get; set; } // Hash for verification
        public DateTime? VerifiedAt { get; set; }
        public int VerificationAttempts { get; set; } = 0;
        
        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        
        // Additional metadata
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Notes { get; set; }
    }
}
