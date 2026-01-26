using real_proxy_api.DTOs;

namespace real_proxy_api.Services
{
    public interface IPaymentService
    {
        /// <summary>
        /// Get authentication token from EPS Payment Gateway
        /// </summary>
        Task<GetTokenResponse> GetTokenAsync();

        /// <summary>
        /// Initialize payment and get redirect URL
        /// </summary>
        Task<InitializePaymentResponse> InitializePaymentAsync(InitializePaymentRequest request, int userId, string? ipAddress = null, string? userAgent = null);

        /// <summary>
        /// Verify transaction status using merchant transaction ID
        /// </summary>
        Task<VerifyTransactionResponse> VerifyTransactionAsync(string merchantTransactionId, string? ipAddress = null);

        /// <summary>
        /// Generate HMACSHA512 hash for API security
        /// </summary>
        string GenerateHash(string data, string hashKey);
    }
}
