namespace real_proxy_api.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string otpCode);
    }
}
