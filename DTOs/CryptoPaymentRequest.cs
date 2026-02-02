namespace real_proxy_api.DTOs
{
    public class CryptoPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? QuoteAssetId { get; set; } = "usd";
    }
}
