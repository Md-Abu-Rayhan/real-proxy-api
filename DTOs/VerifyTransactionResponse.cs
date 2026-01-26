namespace real_proxy_api.DTOs
{
    public class VerifyTransactionResponse
    {
        public string? MerchantTransactionId { get; set; }
        public string? Status { get; set; }
        public string? TotalAmount { get; set; }
        public string? TransactionDate { get; set; }
        public string? TransactionType { get; set; }
        public string? FinancialEntity { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerAddress2 { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerState { get; set; }
        public string? CustomerPostcode { get; set; }
        public string? CustomerCountry { get; set; }
        public string? CustomerPhone { get; set; }
        public string? ShipmentName { get; set; }
        public string? ShipmentAddress { get; set; }
        public string? ShipmentAddress2 { get; set; }
        public string? ShipmentCity { get; set; }
        public string? ShipmentState { get; set; }
        public string? ShipmentPostcode { get; set; }
        public string? ShipmentCountry { get; set; }
        public string? ValueA { get; set; }
        public string? ValueB { get; set; }
        public string? ValueC { get; set; }
        public string? ValueD { get; set; }
        public string? ShippingMethod { get; set; }
        public string? NoOfItem { get; set; }
        public string? ProductName { get; set; }
        public string? ProductProfile { get; set; }
        public string? ProductCategory { get; set; }
    }
}
