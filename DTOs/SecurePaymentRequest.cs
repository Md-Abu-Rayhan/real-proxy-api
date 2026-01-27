namespace real_proxy_api.DTOs
{
    public class SecurePaymentRequest
    {
        public string PackageId { get; set; } = string.Empty;
        public string CustomerOrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public string CustomerCity { get; set; } = string.Empty;
        public string CustomerState { get; set; } = string.Empty;
        public string CustomerPostcode { get; set; } = string.Empty;
        public string CustomerCountry { get; set; } = "BD";
    }
}
