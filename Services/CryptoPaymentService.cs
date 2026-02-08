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

                // Ensure orderId is appended to returnTo so frontend can verify it
                var returnToUrl = returnTo;
                if (!returnToUrl.Contains("orderId="))
                {
                    returnToUrl += (returnToUrl.Contains("?") ? "&" : "?") + "orderId=" + request.OrderId;
                }

                var formData = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("payeeId", payeeId),
                    new KeyValuePair<string, string>("settlementAssetId", settlementAssetId),
                    new KeyValuePair<string, string>("quoteAssetId", request.QuoteAssetId ?? "usd"),
                    new KeyValuePair<string, string>("quoteAmount", request.Amount.ToString("F")),
                    new KeyValuePair<string, string>("orderId", request.OrderId),
                    new KeyValuePair<string, string>("returnTo", returnToUrl),
                    new KeyValuePair<string, string>("callbackUrl", callbackUrl)
                };

                var content = new FormUrlEncodedContent(formData);

                _logger.LogInformation("Creating MixPay one-time payment for OrderId: {OrderId}, Amount: {Amount}, ReturnTo: {ReturnTo}", 
                    request.OrderId, request.Amount, returnToUrl);

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

        public async Task<MixPayVerificationResult> VerifyPaymentAsync(string orderId, decimal? expectedAmount = null, string? expectedAssetId = null)
        {
            var result = new MixPayVerificationResult { IsVerified = false };
            try
            {
                _logger.LogInformation("Verifying MixPay payment for OrderId: {OrderId}", orderId);

                var mixPaySettings = _configuration.GetSection("MixPay");
                var payeeId = mixPaySettings["PayeeId"];

                var response = await _httpClient.GetAsync($"https://api.mixpay.me/v1/payments_result?orderId={orderId}&payeeId={payeeId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("MixPay Results API failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);
                    return result;
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;

                if (root.GetProperty("success").GetBoolean())
                {
                    var data = root.GetProperty("data");
                    result.Status = data.TryGetProperty("status", out var sp) ? sp.GetString() : null;
                    result.TraceId = data.TryGetProperty("traceId", out var tp) ? tp.GetString() : null;
                    result.PayeeId = data.TryGetProperty("payeeId", out var pp) ? pp.GetString() : null;
                    result.QuoteAmount = data.TryGetProperty("quoteAmount", out var qap) ? qap.GetString() : null;
                    result.QuoteAssetId = data.TryGetProperty("quoteAssetId", out var qasp) ? qasp.GetString() : null;
                    result.PaymentAssetId = data.TryGetProperty("paymentAssetId", out var pasp) ? pasp.GetString() : null;
                    result.PaymentAmount = data.TryGetProperty("paymentAmount", out var pamp) ? pamp.GetString() : null;
                    result.Txid = data.TryGetProperty("txid", out var txp) ? txp.GetString() : null;
                    result.BlockExplorerUrl = data.TryGetProperty("blockExplorerUrl", out var bep) ? bep.GetString() : null;

                    // 1. Check status is success
                    bool statusMatch = result.Status == "success";
                    
                    // 2. Check payeeId matches (Security check from guide)
                    bool payeeMatch = result.PayeeId == payeeId;

                    // 3. Check quoteAssetId matches (Security check from guide)
                    bool assetMatch = expectedAssetId == null || result.QuoteAssetId?.ToLower() == expectedAssetId.ToLower();

                    // 4. Check quoteAmount matches (Security check from guide)
                    bool amountMatch = true;
                    if (expectedAmount.HasValue && !string.IsNullOrEmpty(result.QuoteAmount))
                    {
                        if (decimal.TryParse(result.QuoteAmount, out decimal actualAmount))
                        {
                            // Allow for small rounding differences in decimal comparison if necessary, 
                            // but usually these should match exactly as they come from the same source.
                            amountMatch = Math.Abs(actualAmount - expectedAmount.Value) < 0.0001m;
                        }
                        else
                        {
                            amountMatch = false;
                        }
                    }

                    if (statusMatch && payeeMatch && assetMatch && amountMatch)
                    {
                        _logger.LogInformation("MixPay payment verified successfully for OrderId: {OrderId}", orderId);
                        result.IsVerified = true;
                    }
                    else
                    {
                        _logger.LogWarning("MixPay payment verification failed strict checks: Status:{Status}, PayeeMatch:{PayeeMatch}, AssetMatch:{AssetMatch}, AmountMatch:{AmountMatch}", 
                            result.Status, payeeMatch, assetMatch, amountMatch);
                    }
                }

                if (!result.IsVerified)
                {
                    _logger.LogWarning("MixPay payment verification failed or pending for OrderId: {OrderId}. Response: {Response}", orderId, responseContent);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying MixPay payment for OrderId: {OrderId}", orderId);
                return result;
            }
        }
    }
}
