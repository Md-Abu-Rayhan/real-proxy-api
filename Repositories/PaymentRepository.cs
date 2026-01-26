using Dapper;
using MySqlConnector;
using real_proxy_api.Models;
using System.Data;

namespace real_proxy_api.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly MySqlConnection _connection;

        public PaymentRepository(MySqlConnection connection)
        {
            _connection = connection;
        }

        // ==================== Payment CRUD Operations ====================

        public async Task<int> CreatePaymentAsync(Payment payment)
        {
            var sql = @"
                INSERT INTO payments (
                    UserId, CustomerOrderId, MerchantTransactionId, EpsTransactionId,
                    Amount, Currency, TransactionTypeId, Status, PaymentMethod,
                    ProductName, ProductProfile, ProductCategory, Quantity,
                    CustomerName, CustomerEmail, CustomerPhone, CustomerAddress,
                    CustomerCity, CustomerState, CustomerPostcode, CustomerCountry,
                    SuccessUrl, FailUrl, CancelUrl,
                    IpAddress, UserAgent, VerificationHash, VerificationAttempts,
                    CreatedAt, ExpiresAt, ErrorCode, ErrorMessage, Notes
                ) VALUES (
                    @UserId, @CustomerOrderId, @MerchantTransactionId, @EpsTransactionId,
                    @Amount, @Currency, @TransactionTypeId, @Status, @PaymentMethod,
                    @ProductName, @ProductProfile, @ProductCategory, @Quantity,
                    @CustomerName, @CustomerEmail, @CustomerPhone, @CustomerAddress,
                    @CustomerCity, @CustomerState, @CustomerPostcode, @CustomerCountry,
                    @SuccessUrl, @FailUrl, @CancelUrl,
                    @IpAddress, @UserAgent, @VerificationHash, @VerificationAttempts,
                    @CreatedAt, @ExpiresAt, @ErrorCode, @ErrorMessage, @Notes
                );
                SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, payment);
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id)
        {
            var sql = "SELECT * FROM payments WHERE Id = @Id";
            return await _connection.QueryFirstOrDefaultAsync<Payment>(sql, new { Id = id });
        }

        public async Task<Payment?> GetPaymentByMerchantTransactionIdAsync(string merchantTransactionId)
        {
            var sql = "SELECT * FROM payments WHERE MerchantTransactionId = @MerchantTransactionId";
            return await _connection.QueryFirstOrDefaultAsync<Payment>(sql, new { MerchantTransactionId = merchantTransactionId });
        }

        public async Task<Payment?> GetPaymentByCustomerOrderIdAsync(string customerOrderId)
        {
            var sql = "SELECT * FROM payments WHERE CustomerOrderId = @CustomerOrderId";
            return await _connection.QueryFirstOrDefaultAsync<Payment>(sql, new { CustomerOrderId = customerOrderId });
        }

        public async Task<Payment?> GetPaymentByEpsTransactionIdAsync(string epsTransactionId)
        {
            var sql = "SELECT * FROM payments WHERE EpsTransactionId = @EpsTransactionId";
            return await _connection.QueryFirstOrDefaultAsync<Payment>(sql, new { EpsTransactionId = epsTransactionId });
        }

        public async Task<bool> UpdatePaymentStatusAsync(int paymentId, string status, string? paymentMethod = null,
            string? epsTransactionId = null, DateTime? completedAt = null, string? errorCode = null, string? errorMessage = null)
        {
            var sql = @"
                UPDATE payments 
                SET Status = @Status,
                    PaymentMethod = COALESCE(@PaymentMethod, PaymentMethod),
                    EpsTransactionId = COALESCE(@EpsTransactionId, EpsTransactionId),
                    CompletedAt = COALESCE(@CompletedAt, CompletedAt),
                    ErrorCode = @ErrorCode,
                    ErrorMessage = @ErrorMessage,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                Id = paymentId,
                Status = status,
                PaymentMethod = paymentMethod,
                EpsTransactionId = epsTransactionId,
                CompletedAt = completedAt,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                UpdatedAt = DateTime.UtcNow
            });

            return rowsAffected > 0;
        }

        public async Task<bool> MarkPaymentVerifiedAsync(int paymentId, string status, string? paymentMethod)
        {
            var sql = @"
                UPDATE payments 
                SET Status = @Status,
                    PaymentMethod = @PaymentMethod,
                    VerifiedAt = @VerifiedAt,
                    CompletedAt = @CompletedAt,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            var now = DateTime.UtcNow;
            var rowsAffected = await _connection.ExecuteAsync(sql, new
            {
                Id = paymentId,
                Status = status,
                PaymentMethod = paymentMethod,
                VerifiedAt = now,
                CompletedAt = status == "Success" ? now : (DateTime?)null,
                UpdatedAt = now
            });

            return rowsAffected > 0;
        }

        public async Task IncrementVerificationAttemptsAsync(int paymentId)
        {
            var sql = @"
                UPDATE payments 
                SET VerificationAttempts = VerificationAttempts + 1,
                    UpdatedAt = @UpdatedAt
                WHERE Id = @Id";

            await _connection.ExecuteAsync(sql, new { Id = paymentId, UpdatedAt = DateTime.UtcNow });
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsAsync(int userId, int limit = 50, int offset = 0)
        {
            var sql = @"
                SELECT * FROM payments 
                WHERE UserId = @UserId 
                ORDER BY CreatedAt DESC 
                LIMIT @Limit OFFSET @Offset";

            return await _connection.QueryAsync<Payment>(sql, new { UserId = userId, Limit = limit, Offset = offset });
        }

        public async Task<IEnumerable<Payment>> GetUserPaymentsByStatusAsync(int userId, string status, int limit = 50, int offset = 0)
        {
            var sql = @"
                SELECT * FROM payments 
                WHERE UserId = @UserId AND Status = @Status 
                ORDER BY CreatedAt DESC 
                LIMIT @Limit OFFSET @Offset";

            return await _connection.QueryAsync<Payment>(sql, new { UserId = userId, Status = status, Limit = limit, Offset = offset });
        }

        public async Task<bool> MerchantTransactionIdExistsAsync(string merchantTransactionId)
        {
            var sql = "SELECT COUNT(1) FROM payments WHERE MerchantTransactionId = @MerchantTransactionId";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { MerchantTransactionId = merchantTransactionId });
            return count > 0;
        }

        public async Task<bool> CustomerOrderIdExistsAsync(string customerOrderId)
        {
            var sql = "SELECT COUNT(1) FROM payments WHERE CustomerOrderId = @CustomerOrderId";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { CustomerOrderId = customerOrderId });
            return count > 0;
        }

        public async Task<IEnumerable<Payment>> GetExpiredPendingPaymentsAsync(int olderThanMinutes = 1440)
        {
            var sql = @"
                SELECT * FROM payments 
                WHERE Status = 'Pending' 
                AND CreatedAt < @ExpiryTime";

            var expiryTime = DateTime.UtcNow.AddMinutes(-olderThanMinutes);
            return await _connection.QueryAsync<Payment>(sql, new { ExpiryTime = expiryTime });
        }

        public async Task<int> ExpireOldPendingPaymentsAsync(int olderThanMinutes = 1440)
        {
            var sql = @"
                UPDATE payments 
                SET Status = 'Expired', 
                    UpdatedAt = @UpdatedAt 
                WHERE Status = 'Pending' 
                AND CreatedAt < @ExpiryTime";

            var expiryTime = DateTime.UtcNow.AddMinutes(-olderThanMinutes);
            return await _connection.ExecuteAsync(sql, new { ExpiryTime = expiryTime, UpdatedAt = DateTime.UtcNow });
        }

        // ==================== Payment Metadata Operations ====================

        public async Task<int> CreatePaymentMetadataAsync(PaymentMetadata metadata)
        {
            var sql = @"
                INSERT INTO payment_metadata (
                    PaymentId, ValueA, ValueB, ValueC, ValueD,
                    ShipmentName, ShipmentAddress, ShipmentAddress2,
                    ShipmentCity, ShipmentState, ShipmentPostcode, ShipmentCountry,
                    ShippingMethod, ProductListJson, EpsResponseJson, CreatedAt
                ) VALUES (
                    @PaymentId, @ValueA, @ValueB, @ValueC, @ValueD,
                    @ShipmentName, @ShipmentAddress, @ShipmentAddress2,
                    @ShipmentCity, @ShipmentState, @ShipmentPostcode, @ShipmentCountry,
                    @ShippingMethod, @ProductListJson, @EpsResponseJson, @CreatedAt
                );
                SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, metadata);
        }

        public async Task<PaymentMetadata?> GetPaymentMetadataAsync(int paymentId)
        {
            var sql = "SELECT * FROM payment_metadata WHERE PaymentId = @PaymentId";
            return await _connection.QueryFirstOrDefaultAsync<PaymentMetadata>(sql, new { PaymentId = paymentId });
        }

        public async Task<bool> UpdatePaymentMetadataAsync(PaymentMetadata metadata)
        {
            var sql = @"
                UPDATE payment_metadata 
                SET ValueA = @ValueA, ValueB = @ValueB, ValueC = @ValueC, ValueD = @ValueD,
                    ShipmentName = @ShipmentName, ShipmentAddress = @ShipmentAddress,
                    ShipmentAddress2 = @ShipmentAddress2, ShipmentCity = @ShipmentCity,
                    ShipmentState = @ShipmentState, ShipmentPostcode = @ShipmentPostcode,
                    ShipmentCountry = @ShipmentCountry, ShippingMethod = @ShippingMethod,
                    ProductListJson = @ProductListJson, EpsResponseJson = @EpsResponseJson
                WHERE PaymentId = @PaymentId";

            var rowsAffected = await _connection.ExecuteAsync(sql, metadata);
            return rowsAffected > 0;
        }

        // ==================== Payment Log Operations ====================

        public async Task<int> CreatePaymentLogAsync(PaymentLog log)
        {
            var sql = @"
                INSERT INTO payment_logs (
                    PaymentId, Action, PreviousStatus, NewStatus,
                    RequestData, ResponseData, ErrorMessage,
                    IpAddress, UserAgent, CreatedAt
                ) VALUES (
                    @PaymentId, @Action, @PreviousStatus, @NewStatus,
                    @RequestData, @ResponseData, @ErrorMessage,
                    @IpAddress, @UserAgent, @CreatedAt
                );
                SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, log);
        }

        public async Task<IEnumerable<PaymentLog>> GetPaymentLogsAsync(int paymentId)
        {
            var sql = @"
                SELECT * FROM payment_logs 
                WHERE PaymentId = @PaymentId 
                ORDER BY CreatedAt ASC";

            return await _connection.QueryAsync<PaymentLog>(sql, new { PaymentId = paymentId });
        }

        public async Task<IEnumerable<PaymentLog>> GetRecentPaymentLogsAsync(int limit = 100)
        {
            var sql = @"
                SELECT * FROM payment_logs 
                ORDER BY CreatedAt DESC 
                LIMIT @Limit";

            return await _connection.QueryAsync<PaymentLog>(sql, new { Limit = limit });
        }

        // ==================== Analytics & Reporting ====================

        public async Task<PaymentStatistics> GetUserPaymentStatisticsAsync(int userId)
        {
            var sql = @"
                SELECT 
                    COUNT(*) as TotalTransactions,
                    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
                    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
                    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as PendingCount,
                    SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) as CancelledCount,
                    SUM(CASE WHEN Status = 'Expired' THEN 1 ELSE 0 END) as ExpiredCount,
                    COALESCE(SUM(CASE WHEN Status = 'Success' THEN Amount ELSE 0 END), 0) as TotalRevenue,
                    COALESCE(AVG(Amount), 0) as AverageTransactionAmount
                FROM payments
                WHERE UserId = @UserId";

            var stats = await _connection.QueryFirstOrDefaultAsync<PaymentStatistics>(sql, new { UserId = userId });
            
            if (stats != null && stats.TotalTransactions > 0)
            {
                stats.SuccessRate = (decimal)stats.SuccessCount / stats.TotalTransactions * 100;
            }

            return stats ?? new PaymentStatistics();
        }

        public async Task<PaymentStatistics> GetPaymentStatisticsByDateRangeAsync(DateTime startDate, DateTime endDate, int? userId = null)
        {
            var sql = @"
                SELECT 
                    COUNT(*) as TotalTransactions,
                    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
                    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailedCount,
                    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) as PendingCount,
                    SUM(CASE WHEN Status = 'Cancelled' THEN 1 ELSE 0 END) as CancelledCount,
                    SUM(CASE WHEN Status = 'Expired' THEN 1 ELSE 0 END) as ExpiredCount,
                    COALESCE(SUM(CASE WHEN Status = 'Success' THEN Amount ELSE 0 END), 0) as TotalRevenue,
                    COALESCE(AVG(Amount), 0) as AverageTransactionAmount
                FROM payments
                WHERE CreatedAt >= @StartDate AND CreatedAt <= @EndDate";

            if (userId.HasValue)
            {
                sql += " AND UserId = @UserId";
            }

            var stats = await _connection.QueryFirstOrDefaultAsync<PaymentStatistics>(sql, new { StartDate = startDate, EndDate = endDate, UserId = userId });
            
            if (stats != null && stats.TotalTransactions > 0)
            {
                stats.SuccessRate = (decimal)stats.SuccessCount / stats.TotalTransactions * 100;
            }

            return stats ?? new PaymentStatistics();
        }

        public async Task<IEnumerable<Payment>> GetRecentFailedPaymentsAsync(int hours = 24, int limit = 50)
        {
            var sql = @"
                SELECT * FROM payments 
                WHERE Status = 'Failed' 
                AND CreatedAt > @Since 
                ORDER BY CreatedAt DESC 
                LIMIT @Limit";

            var since = DateTime.UtcNow.AddHours(-hours);
            return await _connection.QueryAsync<Payment>(sql, new { Since = since, Limit = limit });
        }

        // ==================== Transaction Management ====================

        public async Task<int> CreatePaymentWithMetadataAsync(Payment payment, PaymentMetadata? metadata, PaymentLog initialLog)
        {
            // Ensure connection is open
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();

            using var transaction = await _connection.BeginTransactionAsync();
            try
            {
                // Create payment with transaction
                var paymentSql = @"
                    INSERT INTO payments (
                        UserId, CustomerOrderId, MerchantTransactionId, EpsTransactionId,
                        Amount, Currency, TransactionTypeId, Status, PaymentMethod,
                        ProductName, ProductProfile, ProductCategory, Quantity,
                        CustomerName, CustomerEmail, CustomerPhone, CustomerAddress,
                        CustomerCity, CustomerState, CustomerPostcode, CustomerCountry,
                        SuccessUrl, FailUrl, CancelUrl,
                        IpAddress, UserAgent, VerificationHash, VerificationAttempts,
                        CreatedAt, ExpiresAt, ErrorCode, ErrorMessage, Notes
                    ) VALUES (
                        @UserId, @CustomerOrderId, @MerchantTransactionId, @EpsTransactionId,
                        @Amount, @Currency, @TransactionTypeId, @Status, @PaymentMethod,
                        @ProductName, @ProductProfile, @ProductCategory, @Quantity,
                        @CustomerName, @CustomerEmail, @CustomerPhone, @CustomerAddress,
                        @CustomerCity, @CustomerState, @CustomerPostcode, @CustomerCountry,
                        @SuccessUrl, @FailUrl, @CancelUrl,
                        @IpAddress, @UserAgent, @VerificationHash, @VerificationAttempts,
                        @CreatedAt, @ExpiresAt, @ErrorCode, @ErrorMessage, @Notes
                    );
                    SELECT LAST_INSERT_ID();";

                var paymentId = await _connection.ExecuteScalarAsync<int>(paymentSql, payment, transaction);
                
                // Create metadata if provided
                if (metadata != null)
                {
                    metadata.PaymentId = paymentId;
                    
                    var metadataSql = @"
                        INSERT INTO payment_metadata (
                            PaymentId, ValueA, ValueB, ValueC, ValueD,
                            ShipmentName, ShipmentAddress, ShipmentAddress2,
                            ShipmentCity, ShipmentState, ShipmentPostcode, ShipmentCountry,
                            ShippingMethod, ProductListJson, EpsResponseJson, CreatedAt
                        ) VALUES (
                            @PaymentId, @ValueA, @ValueB, @ValueC, @ValueD,
                            @ShipmentName, @ShipmentAddress, @ShipmentAddress2,
                            @ShipmentCity, @ShipmentState, @ShipmentPostcode, @ShipmentCountry,
                            @ShippingMethod, @ProductListJson, @EpsResponseJson, @CreatedAt
                        );";
                    
                    await _connection.ExecuteAsync(metadataSql, metadata, transaction);
                }
                
                // Create initial log
                initialLog.PaymentId = paymentId;
                
                var logSql = @"
                    INSERT INTO payment_logs (
                        PaymentId, Action, PreviousStatus, NewStatus,
                        RequestData, ResponseData, ErrorMessage,
                        IpAddress, UserAgent, CreatedAt
                    ) VALUES (
                        @PaymentId, @Action, @PreviousStatus, @NewStatus,
                        @RequestData, @ResponseData, @ErrorMessage,
                        @IpAddress, @UserAgent, @CreatedAt
                    );";
                
                await _connection.ExecuteAsync(logSql, initialLog, transaction);
                
                await transaction.CommitAsync();
                return paymentId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
