namespace real_proxy_api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? InvitationCode { get; set; }
        public string? ProxyAccount { get; set; }
        public string? ProxyPassword { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
