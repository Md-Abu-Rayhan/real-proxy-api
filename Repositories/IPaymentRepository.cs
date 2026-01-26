using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public interface IPaymentRepository
    {
        // ==================== Payment CRUD Operations ====================
        
        /// <summary>
        /// Create a new payment transaction
        /// </summary>
        Task<int> CreatePaymentAsync(Payment payment);
        
        /// <summary>
        /// Get payment by ID
        /// </summary>
        Task<Payment?> GetPaymentByIdAsync(int id);
        
        /// <summary>
        /// Get payment by merchant transaction ID (most common lookup)
        /// </summary>
        Task<Payment?> GetPaymentByMerchantTransactionIdAsync(string merchantTransactionId);
        
        /// <summary>
        /// Get payment by customer order ID
        /// </summary>
        Task<Payment?> GetPaymentByCustomerOrderIdAsync(string customerOrderId);
        
        /// <summary>
        /// Get payment by EPS transaction ID
        /// </summary>
        Task<Payment?> GetPaymentByEpsTransactionIdAsync(string epsTransactionId);
        
        /// <summary>
        /// Update payment status and related fields after verification
        /// </summary>
        Task<bool> UpdatePaymentStatusAsync(int paymentId, string status, string? paymentMethod = null, 
            string? epsTransactionId = null, DateTime? completedAt = null, string? errorCode = null, string? errorMessage = null);
        
        /// <summary>
        /// Mark payment as verified
        /// </summary>
        Task<bool> MarkPaymentVerifiedAsync(int paymentId, string status, string? paymentMethod);
        
        /// <summary>
        /// Update verification attempts counter
        /// </summary>
        Task IncrementVerificationAttemptsAsync(int paymentId);
        
        /// <summary>
        /// Get all payments for a specific user
        /// </summary>
        Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId, int limit = 50, int offset = 0);
        
        /// <summary>
        /// Get user payments by status
        /// </summary>
        Task<IEnumerable<Payment>> GetUserPaymentsByStatusAsync(int userId, string status, int limit = 50, int offset = 0);
        
        /// <summary>
        /// Check if merchant transaction ID already exists (prevent duplicates)
        /// </summary>
        Task<bool> MerchantTransactionIdExistsAsync(string merchantTransactionId);
        
        /// <summary>
        /// Check if customer order ID already exists (prevent duplicates)
        /// </summary>
        Task<bool> CustomerOrderIdExistsAsync(string customerOrderId);
        
        /// <summary>
        /// Get pending payments older than specified minutes (for cleanup/expiry)
        /// </summary>
        Task<IEnumerable<Payment>> GetExpiredPendingPaymentsAsync(int olderThanMinutes = 1440);
        
        /// <summary>
        /// Expire old pending payments
        /// </summary>
        Task<int> ExpireOldPendingPaymentsAsync(int olderThanMinutes = 1440);
        
        // ==================== Payment Log Operations ====================
        
        /// <summary>
        /// Create a payment log entry
        /// </summary>
        Task<int> CreatePaymentLogAsync(PaymentLog log);
        
        /// <summary>
        /// Get all logs for a specific payment
        /// </summary>
        Task<IEnumerable<PaymentLog>> GetPaymentLogsAsync(int paymentId);
        
        /// <summary>
        /// Get recent payment logs (for monitoring)
        /// </summary>
        Task<IEnumerable<PaymentLog>> GetRecentPaymentLogsAsync(int limit = 100);
        
        // ==================== Analytics & Reporting ====================
        
        /// <summary>
        /// Get payment statistics for a user
        /// </summary>
        Task<PaymentStatistics> GetUserPaymentStatisticsAsync(int userId);
        
        /// <summary>
        /// Get payment statistics for a date range
        /// </summary>
        Task<PaymentStatistics> GetPaymentStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate, int? userId = null);
        
        /// <summary>
        /// Get recent failed payments (for monitoring)
        /// </summary>
        Task<IEnumerable<Payment>> GetRecentFailedPaymentsAsync(int hours = 24, int limit = 50);

        /// <summary>
        /// Get additional metadata for a payment
        /// </summary>
        Task<PaymentMetadata?> GetPaymentMetadataAsync(int paymentId);
        
        // ==================== Transaction Management ====================
        
        /// <summary>
        /// Execute multiple operations in a transaction
        /// Used when creating payment with initial log
        /// </summary>
        Task<int> CreatePaymentWithLogAsync(Payment payment, PaymentLog initialLog);
    }
    
    /// <summary>
    /// Payment statistics model for reporting
    /// </summary>
    public class PaymentStatistics
    {
        public int TotalTransactions { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int CancelledCount { get; set; }
        public int ExpiredCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public decimal SuccessRate { get; set; }
    }
}
