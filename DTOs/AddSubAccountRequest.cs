namespace real_proxy_api.DTOs
{
    public class AddSubAccountRequest
    {
        public string Email { get; set; } = string.Empty;
        public decimal Balance { get; set; } = 0;
    }
}
