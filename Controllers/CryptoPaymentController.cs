using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using real_proxy_api.DTOs;
using real_proxy_api.Services;
using real_proxy_api.Repositories;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace real_proxy_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CryptoPaymentController : ControllerBase
    {
        private readonly ICryptoPaymentService _cryptoPaymentService;
        private readonly ICryptoPaymentRepository _cryptoPaymentRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CryptoPaymentController> _logger;

        public CryptoPaymentController(ICryptoPaymentService cryptoPaymentService, ICryptoPaymentRepository cryptoPaymentRepository, IConfiguration configuration, ILogger<CryptoPaymentController> logger)
        {
            _cryptoPaymentService = cryptoPaymentService;
            _cryptoPaymentRepository = cryptoPaymentRepository;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("initialize")]
        [Authorize]
        public async Task<IActionResult> InitializePayment([FromBody] CryptoPaymentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OrderId))
                {
                    request.OrderId = Guid.NewGuid().ToString("N");
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be greater than 0" });
                }

                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Unauthorized(new { message = "User not found in token" });
                }

                var mixPaySettings = _configuration.GetSection("MixPay");
                var payeeId = mixPaySettings["PayeeId"];
                var settlementAssetId = mixPaySettings["SettlementAssetId"];

                var paymentUrl = await _cryptoPaymentService.CreateOneTimePaymentAsync(request);

                // Create initial payment record in DB
                var payment = new real_proxy_api.Models.CryptoPayment
                {
                    UserId = userId,
                    OrderId = request.OrderId,
                    Amount = request.Amount,
                    QuoteAssetId = request.QuoteAssetId ?? "usd",
                    PayeeId = payeeId,
                    SettlementAssetId = settlementAssetId,
                    Status = "Pending",
                    PaymentUrl = paymentUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                int paymentId = await _cryptoPaymentRepository.CreateAsync(payment);

                // Log the initialization
                await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                {
                    CryptoPaymentId = paymentId,
                    Action = "Initialize",
                    NewStatus = "Pending",
                    RequestData = JsonSerializer.Serialize(request),
                    ResponseData = JsonSerializer.Serialize(new { paymentUrl, orderId = request.OrderId }),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                });

                return Ok(new
                {
                    success = true,
                    paymentUrl = paymentUrl,
                    orderId = request.OrderId,
                    message = "Payment initialized successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing crypto payment for OrderId: {OrderId}", request.OrderId);
                return StatusCode(500, new { message = "An error occurred while initializing crypto payment", error = ex.Message });
            }
        }

        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromBody] JsonElement callbackData)
        {
            string? orderId = null;
            real_proxy_api.Models.CryptoPayment? payment = null;

            try
            {
                _logger.LogInformation("MixPay callback received: {Data}", callbackData.GetRawText());

                // MixPay callback sends the payment details including orderId
                if (callbackData.TryGetProperty("data", out var data) && data.TryGetProperty("orderId", out var orderIdProp))
                {
                    orderId = orderIdProp.GetString() ?? "";
                    if (string.IsNullOrEmpty(orderId)) return BadRequest();

                    // 1. Find the payment in DB
                    payment = await _cryptoPaymentRepository.GetByOrderIdAsync(orderId);
                    if (payment == null)
                    {
                        _logger.LogWarning("Callback received for unknown crypto order: {OrderId}", orderId);
                        return NotFound();
                    }

                    // Log callback arrival
                    await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                    {
                        CryptoPaymentId = payment.Id,
                        Action = "CallbackReceived",
                        PreviousStatus = payment.Status,
                        RequestData = callbackData.GetRawText(),
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                    if (payment.Status == "Success")
                    {
                        return Ok(new { code = "SUCCESS" });
                    }

                    // 2. Verify with MixPay Result API (mandatory as per guide)
                    var verificationResult = await _cryptoPaymentService.VerifyPaymentAsync(orderId);

                    if (verificationResult.IsVerified)
                    {
                        // 3. Update status to Success with audit details
                        string oldStatus = payment.Status;
                        await _cryptoPaymentRepository.UpdateStatusAsync(
                            orderId, 
                            "Success", 
                            traceId: verificationResult.TraceId,
                            paymentAssetId: verificationResult.PaymentAssetId,
                            paymentAmount: verificationResult.PaymentAmount,
                            completedAt: DateTime.UtcNow
                        );
                        
                        // Log success with full verification data
                        await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                        {
                            CryptoPaymentId = payment.Id,
                            Action = "VerificationSuccess",
                            PreviousStatus = oldStatus,
                            NewStatus = "Success",
                            ResponseData = JsonSerializer.Serialize(verificationResult),
                            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                        });

                        _logger.LogInformation("Crypto payment successful for OrderId: {OrderId}, TraceId: {TraceId}", orderId, verificationResult.TraceId);
                    }
                    else
                    {
                        await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                        {
                            CryptoPaymentId = payment.Id,
                            Action = "VerificationFailed",
                            PreviousStatus = payment.Status,
                            ErrorMessage = "Verification with MixPay API failed or pending",
                            ResponseData = JsonSerializer.Serialize(verificationResult),
                            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                        });

                        _logger.LogWarning("Crypto payment verification failed for OrderId: {OrderId}", orderId);
                    }
                }

                // Always respond with SUCCESS as per MixPay documentation to stop retries
                return Ok(new { code = "SUCCESS" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MixPay callback");
                
                if (payment != null)
                {
                    await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                    {
                        CryptoPaymentId = payment.Id,
                        Action = "CallbackError",
                        ErrorMessage = ex.Message,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });
                }

                return Ok(new { code = "SUCCESS" });
            }
        }
    }
}
