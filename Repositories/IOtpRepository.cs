using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public interface IOtpRepository
    {
        Task CreateOtpAsync(string email, string otpCode);
        Task<Otp?> GetValidOtpAsync(string email, string otpCode);
        Task DeleteOtpAsync(string email);
    }
}
