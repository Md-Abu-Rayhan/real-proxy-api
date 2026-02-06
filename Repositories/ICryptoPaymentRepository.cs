using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public interface ICryptoPaymentRepository
    {
        Task<int> CreateAsync(CryptoPayment payment);
        Task<CryptoPayment?> GetByOrderIdAsync(string orderId);
        Task<bool> UpdateStatusAsync(string orderId, string status, string? traceId = null, string? paymentAssetId = null, string? paymentAmount = null, DateTime? completedAt = null);
        Task<IEnumerable<CryptoPayment>> GetByUserIdAsync(int userId);
        Task<int> CreateLogAsync(CryptoPaymentLog log);
    }
}
