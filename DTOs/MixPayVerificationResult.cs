namespace real_proxy_api.DTOs
{
    public class MixPayVerificationResult
    {
        public bool IsVerified { get; set; }
        public string? Status { get; set; }
        public string? TraceId { get; set; }
        public string? PayeeId { get; set; }
        public string? QuoteAmount { get; set; }
        public string? QuoteAssetId { get; set; }
        public string? PaymentAssetId { get; set; }
        public string? PaymentAmount { get; set; }
        public string? Txid { get; set; }
        public string? BlockExplorerUrl { get; set; }
    }
}
