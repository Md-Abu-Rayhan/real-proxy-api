using System.Text.Json;

namespace real_proxy_api.Services
{
    public class ProxyService : IProxyService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProxyService> _logger;

        public ProxyService(HttpClient httpClient, IConfiguration configuration, ILogger<ProxyService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool Success, string Message, string? Account, string? Password)> CreateSubAccountAsync(
            string account,
            string password,
            int proxyType,
            string? remark,
            int traffic,
            string trafficUnit,
            int bandwidth,
            int hostname,
            int status)
        {
            var token = "1fba7889-4be8-49fa-a34d-1551a96d2e7d";
            //var token = _configuration["ProxyApi:Token"];
            var key = "F692CNNBuDSWoNUU";
            //var key = _configuration["ProxyApi:Key"];
            var apiUrl = _configuration["ProxyApi:BaseUrl"] ?? "https://docapi.922proxy.com/api/account/create";

            var formData = new Dictionary<string, string>
            {
                { "token", token ?? "" },
                { "key", key ?? "" },
                { "account", account },
                { "password", password },
                { "proxy_type", proxyType.ToString() },
                { "remark", remark ?? "" },
                { "traffic", traffic.ToString() },
                { "traffic_unit", trafficUnit },
                { "bandwidth", bandwidth.ToString() },
                { "hostname", hostname.ToString() },
                { "status", status.ToString() }
            };

            try
            {
                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Proxy API response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(responseContent);
                    var root = jsonDoc.RootElement;

                    // Check if the API response indicates success (code can be string or number: 0 or 200)
                    if (root.TryGetProperty("code", out var codeElement))
                    {
                        var codeValue = codeElement.ValueKind == JsonValueKind.String 
                            ? codeElement.GetString() 
                            : codeElement.GetInt32().ToString();
                        
                        if (codeValue == "0" || codeValue == "200")
                        {
                            return (true, "Sub-account created successfully", account, password);
                        }
                    }

                    var errorMessage = root.TryGetProperty("msg", out var msgElement) 
                        ? msgElement.GetString() 
                        : $"Proxy API error. Response: {responseContent}";
                    return (false, errorMessage ?? "Unknown error", null, null);
                }

                return (false, $"Failed to create sub-account: {responseContent}", null, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error calling proxy API: {ex.Message}", null, null);
            }
        }
    }
}
