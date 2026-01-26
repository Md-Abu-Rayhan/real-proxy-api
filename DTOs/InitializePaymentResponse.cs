namespace real_proxy_api.DTOs
{
    public class InitializePaymentResponse
    {
        public string? TransactionId { get; set; }
        public string? RedirectURL { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public object? FinancialEntityList { get; set; }
    }
}
