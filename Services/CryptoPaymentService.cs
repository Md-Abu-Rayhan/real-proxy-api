using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using real_proxy_api.DTOs;

namespace real_proxy_api.Services
{
    public class CryptoPaymentService : ICryptoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CryptoPaymentService> _logger;

        public CryptoPaymentService(HttpClient httpClient, IConfiguration configuration, ILogger<CryptoPaymentService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> CreateOneTimePaymentAsync(CryptoPaymentRequest request)
        {
            try
            {
                var mixPaySettings = _configuration.GetSection("MixPay");
                var payeeId = mixPaySettings["PayeeId"] ?? throw new InvalidOperationException("MixPay PayeeId not configured");
                var settlementAssetId = mixPaySettings["SettlementAssetId"] ?? throw new InvalidOperationException("MixPay SettlementAssetId not configured");
                var returnTo = mixPaySettings["ReturnTo"] ?? throw new InvalidOperationException("MixPay ReturnTo not configured");
                var callbackUrl = mixPaySettings["CallbackUrl"] ?? throw new InvalidOperationException("MixPay CallbackUrl not configured");

                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("payeeId", payeeId),
                    new KeyValuePair<string, string>("settlementAssetId", settlementAssetId),
                    new KeyValuePair<string, string>("quoteAssetId", request.QuoteAssetId ?? "usd"),
                    new KeyValuePair<string, string>("quoteAmount", request.Amount.ToString("F")),
                    new KeyValuePair<string, string>("orderId", request.OrderId),
                    new KeyValuePair<string, string>("returnTo", returnTo),
                    new KeyValuePair<string, string>("callbackUrl", callbackUrl)
                };

                var content = new FormUrlEncodedContent(formData);

                _logger.LogInformation("Creating MixPay one-time payment for OrderId: {OrderId}, Amount: {Amount}", request.OrderId, request.Amount);

                var response = await _httpClient.PostAsync("https://api.mixpay.me/v1/one_time_payment", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("MixPay API fallback failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);
                    throw new Exception($"MixPay API error: {response.StatusCode}");
                }

                var mixPayResponse = JsonSerializer.Deserialize<MixPayResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (mixPayResponse == null || !mixPayResponse.Success || mixPayResponse.Data == null)
                {
                    _logger.LogError("MixPay API returned unsuccessful response: {Response}", responseContent);
                    throw new Exception(mixPayResponse?.Message ?? "MixPay API error");
                }

                var paymentCode = mixPayResponse.Data.Code;
                var paymentUrl = $"https://mixpay.me/code/{paymentCode}";

                _logger.LogInformation("MixPay payment created successfully. Payment URL: {PaymentUrl}", paymentUrl);

                return paymentUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating MixPay payment for OrderId: {OrderId}", request.OrderId);
                throw;
            }
        }
    }
}
