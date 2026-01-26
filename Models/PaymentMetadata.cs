namespace real_proxy_api.Models
{
    /// <summary>
    /// Extended payment information and custom fields
    /// </summary>
    public class PaymentMetadata
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        
        // Custom Values (EPS API support)
        public string? ValueA { get; set; }
        public string? ValueB { get; set; }
        public string? ValueC { get; set; }
        public string? ValueD { get; set; }
        
        // Shipping Information
        public string? ShipmentName { get; set; }
        public string? ShipmentAddress { get; set; }
        public string? ShipmentAddress2 { get; set; }
        public string? ShipmentCity { get; set; }
        public string? ShipmentState { get; set; }
        public string? ShipmentPostcode { get; set; }
        public string? ShipmentCountry { get; set; }
        public string? ShippingMethod { get; set; }
        
        // Product List JSON
        public string? ProductListJson { get; set; }
        
        // EPS Response Data
        public string? EpsResponseJson { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
