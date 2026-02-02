namespace real_proxy_api.DTOs
{
    public class MixPayResponse
    {
        public int Code { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public MixPayData? Data { get; set; }
        public long TimestampMs { get; set; }
    }

    public class MixPayData
    {
        public string Code { get; set; } = string.Empty;
        public MixPayInfo? Info { get; set; }
    }

    public class MixPayInfo
    {
        public string PayeeId { get; set; } = string.Empty;
        public string SettlementAssetId { get; set; } = string.Empty;
        public string QuoteAssetId { get; set; } = string.Empty;
        public string QuoteAmount { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public string ReturnTo { get; set; } = string.Empty;
    }
}
