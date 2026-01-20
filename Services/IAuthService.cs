using real_proxy_api.DTOs;

namespace real_proxy_api.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request);
        Task<(bool Success, string Message, string? Token)> LoginAsync(LoginRequest request);
        Task<(bool Success, string Message)> SendOtpAsync(ForgetPasswordRequest request);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
