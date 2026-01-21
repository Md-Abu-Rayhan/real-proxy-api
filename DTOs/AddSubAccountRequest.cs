namespace real_proxy_api.DTOs
{
    public class AddSubAccountRequest
    {
        public string Email { get; set; } = string.Empty;
        public int ProxyType { get; set; } = 1;
        public string? Remark { get; set; }
        public int Traffic { get; set; } = 10;
        public string TrafficUnit { get; set; } = "MB";
        public int Bandwidth { get; set; } = 0;
        public int Hostname { get; set; } = 0;
        public int Status { get; set; } = 1;
    }
}
