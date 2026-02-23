namespace real_proxy_api.Models
{
    public class ProxyPurchaseLog
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
