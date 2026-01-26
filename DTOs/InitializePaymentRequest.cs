namespace real_proxy_api.DTOs
{
    public class InitializePaymentRequest
    {
        public string MerchantId { get; set; } = string.Empty;
        public string StoreId { get; set; } = string.Empty;
        public string CustomerOrderId { get; set; } = string.Empty;
        public string MerchantTransactionId { get; set; } = string.Empty;
        public int TransactionTypeId { get; set; } = 1; // 1=Web, 2=Android, 3=IOS
        public decimal TotalAmount { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string FailUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string? CustomerAddress2 { get; set; }
        public string CustomerCity { get; set; } = string.Empty;
        public string CustomerState { get; set; } = string.Empty;
        public string CustomerPostcode { get; set; } = string.Empty;
        public string CustomerCountry { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
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
        public string ProductName { get; set; } = string.Empty;
        public string? ProductProfile { get; set; }
        public string? ProductCategory { get; set; }
        public List<ProductItem>? ProductList { get; set; }
    }

    public class ProductItem
    {
        public string ProductName { get; set; } = string.Empty;
        public string NoOfItem { get; set; } = string.Empty;
        public string? ProductProfile { get; set; }
        public string? ProductCategory { get; set; }
        public string ProductPrice { get; set; } = string.Empty;
    }
}
