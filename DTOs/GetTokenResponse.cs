namespace real_proxy_api.DTOs
{
    public class GetTokenResponse
    {
        public string? Token { get; set; }
        public string? ExpireDate { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
    }
}
