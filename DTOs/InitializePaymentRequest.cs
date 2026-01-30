using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class InitializePaymentRequest
    {
        [JsonPropertyName("merchantId")]
        public string MerchantId { get; set; } = string.Empty;

        [JsonPropertyName("storeId")]
        public string StoreId { get; set; } = string.Empty;

        [JsonPropertyName("CustomerOrderId")]
        public string CustomerOrderId { get; set; } = string.Empty;

        [JsonPropertyName("merchantTransactionId")]
        public string MerchantTransactionId { get; set; } = string.Empty;

        [JsonPropertyName("transactionTypeId")]
        public int TransactionTypeId { get; set; } = 1; // 1=Web

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("financialEntityId")]
        public int FinancialEntityId { get; set; } = 0;

        [JsonPropertyName("transitionStatusId")]
        public int TransitionStatusId { get; set; } = 0;

        [JsonPropertyName("ipAddress")]
        public string? IpAddress { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; } = "1";

        [JsonPropertyName("successUrl")]
        public string SuccessUrl { get; set; } = string.Empty;

        [JsonPropertyName("failUrl")]
        public string FailUrl { get; set; } = string.Empty;

        [JsonPropertyName("cancelUrl")]
        public string CancelUrl { get; set; } = string.Empty;

        [JsonPropertyName("customerName")]
        public string CustomerName { get; set; } = string.Empty;

        [JsonPropertyName("customerEmail")]
        public string CustomerEmail { get; set; } = string.Empty;

        [JsonPropertyName("CustomerAddress")]
        public string CustomerAddress { get; set; } = string.Empty;

        [JsonPropertyName("CustomerAddress2")]
        public string? CustomerAddress2 { get; set; }

        [JsonPropertyName("CustomerCity")]
        public string CustomerCity { get; set; } = string.Empty;

        [JsonPropertyName("CustomerState")]
        public string CustomerState { get; set; } = string.Empty;

        [JsonPropertyName("CustomerPostcode")]
        public string CustomerPostcode { get; set; } = string.Empty;

        [JsonPropertyName("CustomerCountry")]
        public string CustomerCountry { get; set; } = string.Empty;

        [JsonPropertyName("CustomerPhone")]
        public string CustomerPhone { get; set; } = string.Empty;

        [JsonPropertyName("ShipmentName")]
        public string? ShipmentName { get; set; }

        [JsonPropertyName("ShipmentAddress")]
        public string? ShipmentAddress { get; set; }

        [JsonPropertyName("ShipmentAddress2")]
        public string? ShipmentAddress2 { get; set; }

        [JsonPropertyName("ShipmentCity")]
        public string? ShipmentCity { get; set; }

        [JsonPropertyName("ShipmentState")]
        public string? ShipmentState { get; set; }

        [JsonPropertyName("ShipmentPostcode")]
        public string? ShipmentPostcode { get; set; }

        [JsonPropertyName("ShipmentCountry")]
        public string? ShipmentCountry { get; set; }

        [JsonPropertyName("valueA")]
        public string? ValueA { get; set; }

        [JsonPropertyName("valueB")]
        public string? ValueB { get; set; }

        [JsonPropertyName("valueC")]
        public string? ValueC { get; set; }

        [JsonPropertyName("valueD")]
        public string? ValueD { get; set; }

        [JsonPropertyName("shippingMethod")]
        public string? ShippingMethod { get; set; }

        [JsonPropertyName("noOfItem")]
        public string? NoOfItem { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("productProfile")]
        public string? ProductProfile { get; set; }

        [JsonPropertyName("productCategory")]
        public string? ProductCategory { get; set; }

        [JsonPropertyName("productList")]
        public List<ProductItem>? ProductList { get; set; }
    }

    public class ProductItem
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("noOfItem")]
        public string NoOfItem { get; set; } = string.Empty;

        [JsonPropertyName("productProfile")]
        public string? ProductProfile { get; set; }

        [JsonPropertyName("productCategory")]
        public string? ProductCategory { get; set; }

        [JsonPropertyName("productPrice")]
        public string ProductPrice { get; set; } = string.Empty;
    }
}
