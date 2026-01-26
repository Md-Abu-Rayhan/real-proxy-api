namespace real_proxy_api.Models
{
    /// <summary>
    /// Additional payment metadata and custom fields
    /// Stores extended information that doesn't fit in main payment table
    /// </summary>
    public class PaymentMetadata
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        
        // Custom Values (as per EPS API)
        public string? ValueA { get; set; }
        public string? ValueB { get; set; }
        public string? ValueC { get; set; }
        public string? ValueD { get; set; }
        
        // Shipping Information (optional for physical products)
        public string? ShipmentName { get; set; }
        public string? ShipmentAddress { get; set; }
        public string? ShipmentAddress2 { get; set; }
        public string? ShipmentCity { get; set; }
        public string? ShipmentState { get; set; }
        public string? ShipmentPostcode { get; set; }
        public string? ShipmentCountry { get; set; }
        public string? ShippingMethod { get; set; }
        
        // Product List (JSON for multiple items)
        public string? ProductListJson { get; set; }
        
        // EPS Response Data (for reference)
        public string? EpsResponseJson { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
