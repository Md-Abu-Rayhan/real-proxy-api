using real_proxy_api.DTOs;

namespace real_proxy_api.Services
{
    public interface ICryptoPaymentService
    {
        Task<string> CreateOneTimePaymentAsync(CryptoPaymentRequest request);
        Task<MixPayVerificationResult> VerifyPaymentAsync(string orderId, decimal? expectedAmount = null, string? expectedAssetId = null);
    }
}
