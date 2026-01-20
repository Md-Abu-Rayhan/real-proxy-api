namespace real_proxy_api.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? InvitationCode { get; set; }
    }
}
