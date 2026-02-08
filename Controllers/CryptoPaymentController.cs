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

        [HttpGet("verify/{orderId}")]
        [Authorize]
        public async Task<IActionResult> Verify(string orderId)
        {
            try
            {
                _logger.LogInformation("Manual verification requested for OrderId: {OrderId}", orderId);
                var payment = await _cryptoPaymentRepository.GetByOrderIdAsync(orderId);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                // Verify user owns this payment
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId) || payment.UserId != userId)
                {
                    return Forbid();
                }

                if (payment.Status == "Success")
                {
                    return Ok(new { success = true, status = "Success", payment });
                }

                // Trigger verification with MixPay API
                var verificationResult = await _cryptoPaymentService.VerifyPaymentAsync(orderId, payment.Amount, payment.QuoteAssetId);

                if (verificationResult.IsVerified)
                {
                    _logger.LogInformation("Verification successful for OrderId: {OrderId}", orderId);
                    await _cryptoPaymentRepository.UpdateStatusAsync(
                        orderId,
                        "Success",
                        traceId: verificationResult.TraceId,
                        paymentAssetId: verificationResult.PaymentAssetId,
                        paymentAmount: verificationResult.PaymentAmount,
                        txid: verificationResult.Txid,
                        blockExplorerUrl: verificationResult.BlockExplorerUrl,
                        completedAt: DateTime.UtcNow
                    );

                    // Re-fetch updated payment
                    payment = await _cryptoPaymentRepository.GetByOrderIdAsync(orderId);
                    
                    await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                    {
                        CryptoPaymentId = payment!.Id,
                        Action = "ManualVerifySuccess",
                        NewStatus = "Success",
                        ResponseData = JsonSerializer.Serialize(verificationResult),
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                    return Ok(new { success = true, status = "Success", payment });
                }

                _logger.LogWarning("Verification failed for OrderId: {OrderId}. Result: {Result}", orderId, JsonSerializer.Serialize(verificationResult));
                
                return Ok(new { success = false, status = payment.Status, message = "Payment not yet verified or failed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying crypto payment for OrderId: {OrderId}", orderId);
                return StatusCode(500, new { message = "An error occurred while verifying payment", error = ex.Message });
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
                var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation("MixPay callback received from {IP}: {Data}", remoteIp, callbackData.GetRawText());

                // Security: IP Whitelisting as per guide (Section 2.3.7)
                // MixPay IP: 52.198.117.57
                // Allow localhost for development
                if (remoteIp != "52.198.117.57" && remoteIp != "::1" && remoteIp != "127.0.0.1" && !remoteIp!.StartsWith("192.168."))
                {
                    _logger.LogWarning("Unauthorized MixPay callback attempt from IP: {IP}", remoteIp);
                    // Still return success to avoid letting attacker know it failed, but don't process
                    return Ok(new { code = "SUCCESS" });
                }

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
                        return Ok(new { code = "SUCCESS" }); // Still return success per guide 3.4.2
                    }

                    // Log callback arrival
                    await _cryptoPaymentRepository.CreateLogAsync(new real_proxy_api.Models.CryptoPaymentLog
                    {
                        CryptoPaymentId = payment.Id,
                        Action = "CallbackReceived",
                        PreviousStatus = payment.Status,
                        RequestData = callbackData.GetRawText(),
                        IpAddress = remoteIp
                    });

                    if (payment.Status == "Success")
                    {
                        return Ok(new { code = "SUCCESS" });
                    }

                    // 2. Verify with MixPay Result API (mandatory as per guide 2.4.3)
                    // We pass the expected amount and assetId for strict verification
                    var verificationResult = await _cryptoPaymentService.VerifyPaymentAsync(orderId, payment.Amount, payment.QuoteAssetId);

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
                            txid: verificationResult.Txid,
                            blockExplorerUrl: verificationResult.BlockExplorerUrl,
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
