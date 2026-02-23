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


        public async Task<(bool Success, string Message, string? ResidentialProxyKey, object? Data)> CreateEvomiSubUserAsync(string username, string email, decimal balance)
        {
            var apiKey = _configuration.GetConnectionString("EvomiApiKey");
            if (string.IsNullOrEmpty(apiKey)) apiKey = "xrLkWmoX8AFx6G72ipvU";

            var apiUrl = "https://reseller.evomi.com/v2/reseller/sub_users/create";

            var payload = new
            {
                username = username,
                email = email,
                balance = balance
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Put, apiUrl);
                request.Headers.Add("X-API-KEY", apiKey);
                request.Content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("Evomi API response: {Response}", responseContent);

                if (response.IsSuccessStatusCode)
                {
                    string? proxyKey = null;
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(responseContent);
                        var root = jsonDoc.RootElement;
                        proxyKey = root.GetProperty("data")
                                       .GetProperty("products")
                                       .GetProperty("residential")
                                       .GetProperty("proxy_key")
                                       .GetString();
                    }
                    catch
                    {
                        // Ignore parsing errors for proxy key
                    }

                    try
                    {
                        var data = JsonSerializer.Deserialize<object>(responseContent);
                        return (true, "Account created successfully", proxyKey, data);
                    }
                    catch
                    {
                        return (true, "Account created successfully", proxyKey, responseContent);
                    }
                }

                return (false, $"Failed to create Evomi sub-account: {responseContent}", null, null);
            }
            catch (Exception ex)
            {
                return (false, $"Error calling Evomi API: {ex.Message}", null, null);
            }
        }
    }
}
