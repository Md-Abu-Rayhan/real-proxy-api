namespace real_proxy_api.Models
{
    public class Otp
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
