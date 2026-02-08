using Dapper;
using MySqlConnector;
using real_proxy_api.Models;
using System.Data;

namespace real_proxy_api.Repositories
{
    public class CryptoPaymentRepository : ICryptoPaymentRepository
    {
        private readonly MySqlConnection _connection;

        public CryptoPaymentRepository(MySqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<int> CreateAsync(CryptoPayment payment)
        {
            var sql = @"
                INSERT INTO crypto_payments (
                    UserId, OrderId, Amount, QuoteAssetId, PayeeId, SettlementAssetId,
                    Status, PaymentCode, PaymentUrl, CreatedAt, UpdatedAt
                ) VALUES (
                    @UserId, @OrderId, @Amount, @QuoteAssetId, @PayeeId, @SettlementAssetId,
                    @Status, @PaymentCode, @PaymentUrl, @CreatedAt, @UpdatedAt
                );
                SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, payment);
        }

        public async Task<CryptoPayment?> GetByOrderIdAsync(string orderId)
        {
            var sql = "SELECT * FROM crypto_payments WHERE OrderId = @OrderId";
            return await _connection.QueryFirstOrDefaultAsync<CryptoPayment>(sql, new { OrderId = orderId });
        }

        public async Task<bool> UpdateStatusAsync(string orderId, string status, string? traceId = null, string? paymentAssetId = null, string? paymentAmount = null, string? txid = null, string? blockExplorerUrl = null, DateTime? completedAt = null)
        {
            var sql = @"
                UPDATE crypto_payments 
                SET Status = @Status, 
                    TraceId = COALESCE(@TraceId, TraceId),
                    PaymentAssetId = COALESCE(@PaymentAssetId, PaymentAssetId),
                    PaymentAmount = COALESCE(@PaymentAmount, PaymentAmount),
                    Txid = COALESCE(@Txid, Txid),
                    BlockExplorerUrl = COALESCE(@BlockExplorerUrl, BlockExplorerUrl),
                    CompletedAt = COALESCE(@CompletedAt, CompletedAt), 
                    UpdatedAt = @UpdatedAt 
                WHERE OrderId = @OrderId";

            var rows = await _connection.ExecuteAsync(sql, new 
            { 
                OrderId = orderId, 
                Status = status, 
                TraceId = traceId,
                PaymentAssetId = paymentAssetId,
                PaymentAmount = paymentAmount,
                Txid = txid,
                BlockExplorerUrl = blockExplorerUrl,
                CompletedAt = completedAt, 
                UpdatedAt = DateTime.UtcNow 
            });

            return rows > 0;
        }

        public async Task<IEnumerable<CryptoPayment>> GetByUserIdAsync(int userId)
        {
            var sql = "SELECT * FROM crypto_payments WHERE UserId = @UserId ORDER BY CreatedAt DESC";
            return await _connection.QueryAsync<CryptoPayment>(sql, new { UserId = userId });
        }

        public async Task<int> CreateLogAsync(CryptoPaymentLog log)
        {
            var sql = @"
                INSERT INTO crypto_payment_logs (
                    CryptoPaymentId, Action, PreviousStatus, NewStatus,
                    RequestData, ResponseData, ErrorMessage, IpAddress, CreatedAt
                ) VALUES (
                    @CryptoPaymentId, @Action, @PreviousStatus, @NewStatus,
                    @RequestData, @ResponseData, @ErrorMessage, @IpAddress, @CreatedAt
                );
                SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, log);
        }
    }
}
