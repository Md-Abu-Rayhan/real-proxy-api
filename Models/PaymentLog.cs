namespace real_proxy_api.Models
{
    /// <summary>
    /// Payment activity log for audit trail and debugging
    /// Tracks all state changes and API interactions
    /// </summary>
    public class PaymentLog
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        
        // Log Details
        public string Action { get; set; } = string.Empty; // Initialize, Callback, Verify, StatusChange, etc.
        public string? PreviousStatus { get; set; }
        public string? NewStatus { get; set; }
        
        // Request/Response Data
        public string? RequestData { get; set; } // JSON of request
        public string? ResponseData { get; set; } // JSON of response
        public string? ErrorMessage { get; set; }
        
        // Context
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        
        // Timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
