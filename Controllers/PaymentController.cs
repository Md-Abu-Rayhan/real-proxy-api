using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using real_proxy_api.DTOs;
using real_proxy_api.Repositories;
using real_proxy_api.Services;
using System.Security.Claims;

namespace real_proxy_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ILogger<PaymentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IProxyService _proxyService;
        private readonly IUserRepository _userRepository;

        public PaymentController(
            IPaymentService paymentService, 
            IPaymentRepository paymentRepository, 
            ILogger<PaymentController> logger, 
            IConfiguration configuration,
            IProxyService proxyService,
            IUserRepository userRepository)
        {
            _paymentService = paymentService;
            _paymentRepository = paymentRepository;
            _logger = logger;
            _configuration = configuration;
            _proxyService = proxyService;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Helper method to get user ID from JWT token
        /// </summary>
        private int? GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        /// <summary>
        /// Helper method to get client IP address
        /// </summary>
        private string? GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        /// <summary>
        /// Helper method to get user agent
        /// </summary>
        private string? GetUserAgent()
        {
            return HttpContext.Request.Headers["User-Agent"].ToString();
        }

        /// <summary>
        /// Get authentication token from EPS Payment Gateway
        /// </summary>
        [HttpPost("get-token")]
        public async Task<IActionResult> GetToken()
        {
            try
            {
                var result = await _paymentService.GetTokenAsync();

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    return BadRequest(new { message = result.ErrorMessage, errorCode = result.ErrorCode });
                }

                return Ok(new
                {
                    token = result.Token,
                    expireDate = result.ExpireDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment token");
                return StatusCode(500, new { message = "An error occurred while getting payment token" });
            }
        }

        /// <summary>
        /// Initialize payment and get redirect URL for EPS Payment Gateway
        /// Requires authentication
        /// </summary>
        [HttpPost("initialize")]
        [Authorize]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentRequest request)
        {
            try
            {
                // Get user ID from token
                var userId = GetUserIdFromToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing authentication token" });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(request.CustomerOrderId))
                {
                    return BadRequest(new { message = "CustomerOrderId is required" });
                }

                if (string.IsNullOrWhiteSpace(request.MerchantTransactionId))
                {
                    return BadRequest(new { message = "MerchantTransactionId is required and must be unique" });
                }

                if (request.MerchantTransactionId.Length < 10)
                {
                    return BadRequest(new { message = "MerchantTransactionId must be at least 10 characters long" });
                }

                if (request.TotalAmount <= 0)
                {
                    return BadRequest(new { message = "TotalAmount must be greater than 0" });
                }

                if (string.IsNullOrWhiteSpace(request.CustomerName) || 
                    string.IsNullOrWhiteSpace(request.CustomerEmail) ||
                    string.IsNullOrWhiteSpace(request.CustomerPhone))
                {
                    return BadRequest(new { message = "Customer details (Name, Email, Phone) are required" });
                }

                if (string.IsNullOrWhiteSpace(request.ProductName))
                {
                    return BadRequest(new { message = "ProductName is required" });
                }

                // Get client info for security
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var result = await _paymentService.InitializePaymentAsync(request, userId.Value, ipAddress, userAgent);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    return BadRequest(new 
                    { 
                        message = result.ErrorMessage, 
                        errorCode = result.ErrorCode 
                    });
                }

                return Ok(new
                {
                    transactionId = result.TransactionId,
                    redirectUrl = result.RedirectURL,
                    merchantTransactionId = request.MerchantTransactionId,
                    message = "Payment initialized successfully. Redirect user to the provided URL."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment");
                return StatusCode(500, new { message = "An error occurred while initializing payment" });
            }
        }

        /// <summary>
        /// Initialize payment securely by looking up the price on the server
        /// Requires authentication
        /// </summary>
        [HttpPost("initialize-secure")]
        [Authorize]
        public async Task<IActionResult> InitializeSecurePayment([FromBody] SecurePaymentRequest request)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing authentication token" });
                }

                if (string.IsNullOrWhiteSpace(request.PackageId))
                {
                    return BadRequest(new { message = "PackageId is required" });
                }

                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var result = await _paymentService.InitializeSecurePaymentAsync(request, userId.Value, ipAddress, userAgent);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    return BadRequest(new { message = result.ErrorMessage, errorCode = result.ErrorCode });
                }

                return Ok(new
                {
                    transactionId = result.TransactionId,
                    redirectUrl = result.RedirectURL,
                    message = "Secure payment initialized successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing secure payment");
                return StatusCode(500, new { message = "An error occurred while initializing secure payment" });
            }
        }

        /// <summary>
        /// Verify transaction status using merchant transaction ID
        /// Requires authentication
        /// </summary>
        [HttpGet("verify/{merchantTransactionId}")]
        [Authorize]
        public async Task<IActionResult> VerifyTransaction(string merchantTransactionId)
        {
            try
            {
                // Get user ID from token
                var userId = GetUserIdFromToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing authentication token" });
                }

                if (string.IsNullOrWhiteSpace(merchantTransactionId))
                {
                    return BadRequest(new { message = "MerchantTransactionId is required" });
                }

                // Verify user owns this payment
                var payment = await _paymentRepository.GetPaymentByMerchantTransactionIdAsync(merchantTransactionId);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment transaction not found" });
                }

                if (payment.UserId != userId.Value)
                {
                    return Forbid(); // User doesn't own this payment
                }

                var ipAddress = GetClientIpAddress();
                var result = await _paymentService.VerifyTransactionAsync(merchantTransactionId, ipAddress);

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    return BadRequest(new 
                    { 
                        message = result.ErrorMessage, 
                        errorCode = result.ErrorCode 
                    });
                }

                return Ok(new
                {
                    merchantTransactionId = result.MerchantTransactionId,
                    status = result.Status,
                    totalAmount = result.TotalAmount,
                    transactionDate = result.TransactionDate,
                    transactionType = result.TransactionType,
                    financialEntity = result.FinancialEntity,
                    customerInfo = new
                    {
                        customerId = result.CustomerId,
                        name = result.CustomerName,
                        email = result.CustomerEmail,
                        phone = result.CustomerPhone,
                        address = result.CustomerAddress,
                        city = result.CustomerCity,
                        state = result.CustomerState,
                        postcode = result.CustomerPostcode,
                        country = result.CustomerCountry
                    },
                    productInfo = new
                    {
                        name = result.ProductName,
                        profile = result.ProductProfile,
                        category = result.ProductCategory,
                        noOfItem = result.NoOfItem
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying transaction");
                return StatusCode(500, new { message = "An error occurred while verifying transaction", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Handle callback from EPS Payment Gateway (Success)
        /// This endpoint receives query parameters from the payment gateway after successful payment
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpGet("callback/success")]
        public async Task<IActionResult> PaymentSuccess([FromQuery] string? transactionId, [FromQuery] string? merchantTransactionId)
        {
            try
            {
                _logger.LogInformation("Payment success callback received. TransactionId: {TransactionId}, MerchantTransactionId: {MerchantTransactionId}", 
                    transactionId, merchantTransactionId);

                if (string.IsNullOrWhiteSpace(merchantTransactionId))
                {
                    return BadRequest(new { message = "MerchantTransactionId is required" });
                }

                // Get payment from database
                var payment = await _paymentRepository.GetPaymentByMerchantTransactionIdAsync(merchantTransactionId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for callback. MerchantTransactionId: {MerchantTransactionId}", merchantTransactionId);
                    return NotFound(new { message = "Payment transaction not found" });
                }

                // Log the callback
                await _paymentRepository.CreatePaymentLogAsync(new Models.PaymentLog
                {
                    PaymentId = payment.Id,
                    Action = "SuccessCallback",
                    PreviousStatus = payment.Status,
                    ResponseData = $"TransactionId: {transactionId}, MerchantTransactionId: {merchantTransactionId}",
                    IpAddress = GetClientIpAddress(),
                    CreatedAt = DateTime.UtcNow
                });

                // Automatically verify the transaction
                var ipAddress = GetClientIpAddress();
                var verifyResult = await _paymentService.VerifyTransactionAsync(merchantTransactionId, ipAddress);

                string frontendUrl = _configuration["EPS:FrontendUrl"] ?? "http://localhost:3002";
                
                if (verifyResult.Status == "Success")
                {
                    try
                    {
                        // 1 gb 125 taka if user pay 125 taka then add balance 1000
                        // if user pay 625 taka then add balance 5000
                        decimal balanceToAdd = payment.Amount * 8;

                        var user = await _userRepository.GetUserByIdAsync(payment.UserId);
                        if (user != null && !string.IsNullOrEmpty(user.ProxyAccount))
                        {
                            var evomiResult = await _proxyService.GiveBalanceAsync(user.ProxyAccount, balanceToAdd);
                            
                            if (evomiResult.Success)
                            {
                                await _paymentRepository.CreateProxyPurchaseAsync(new real_proxy_api.Models.ProxyPurchase
                                {
                                    Username = user.ProxyAccount,
                                    Email = user.Email,
                                    Amount = payment.Amount,
                                    Balance = balanceToAdd,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                await _paymentRepository.CreateProxyPurchaseLogAsync(new real_proxy_api.Models.ProxyPurchaseLog
                                {
                                    Username = user.ProxyAccount,
                                    Amount = payment.Amount,
                                    Balance = balanceToAdd,
                                    ErrorMessage = evomiResult.Message,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing proxy balance after successful payment");
                    }

                    return Redirect($"{frontendUrl}/payment/success?merchantTransactionId={merchantTransactionId}&status=Success");
                }
                else
                {
                    return Redirect($"{frontendUrl}/payment/failed?merchantTransactionId={merchantTransactionId}&error=VerificationFailed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment success callback");
                string frontendUrl = _configuration["EPS:FrontendUrl"] ?? "http://localhost:3002";
                return Redirect($"{frontendUrl}/payment/failed?error=InternalError");
            }
        }

        /// <summary>
        /// Handle callback from EPS Payment Gateway (Failed)
        /// This endpoint receives query parameters from the payment gateway after failed payment
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpGet("callback/fail")]
        public async Task<IActionResult> PaymentFailed([FromQuery] string? transactionId, [FromQuery] string? merchantTransactionId)
        {
            try
            {
                _logger.LogWarning("Payment failed callback received. TransactionId: {TransactionId}, MerchantTransactionId: {MerchantTransactionId}", 
                    transactionId, merchantTransactionId);

                if (!string.IsNullOrWhiteSpace(merchantTransactionId))
                {
                    var payment = await _paymentRepository.GetPaymentByMerchantTransactionIdAsync(merchantTransactionId);
                    if (payment != null)
                    {
                        // Log the callback
                        await _paymentRepository.CreatePaymentLogAsync(new Models.PaymentLog
                        {
                            PaymentId = payment.Id,
                            Action = "FailCallback",
                            PreviousStatus = payment.Status,
                            NewStatus = "Failed",
                            ResponseData = $"TransactionId: {transactionId}, MerchantTransactionId: {merchantTransactionId}",
                            IpAddress = GetClientIpAddress(),
                            CreatedAt = DateTime.UtcNow
                        });

                        // Update payment status
                        await _paymentRepository.UpdatePaymentStatusAsync(payment.Id, "Failed", 
                            errorCode: "PAYMENT_FAILED", 
                            errorMessage: "Payment was declined or failed");
                    }
                }

                string frontendUrl = _configuration["EPS:FrontendUrl"] ?? "http://localhost:3002";
                return Redirect($"{frontendUrl}/payment/failed?merchantTransactionId={merchantTransactionId}&status=Failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment fail callback");
                string frontendUrl = _configuration["EPS:FrontendUrl"] ?? "http://localhost:3002";
                return Redirect($"{frontendUrl}/payment/failed?error=InternalError");
            }
        }

        /// <summary>
        /// Handle callback from EPS Payment Gateway (Cancelled)
        /// This endpoint receives query parameters from the payment gateway after user cancels payment
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpGet("callback/cancel")]
        public async Task<IActionResult> PaymentCancelled([FromQuery] string? transactionId, [FromQuery] string? merchantTransactionId)
        {
            try
            {
                _logger.LogInformation("Payment cancelled callback received. TransactionId: {TransactionId}, MerchantTransactionId: {MerchantTransactionId}", 
                    transactionId, merchantTransactionId);

                if (!string.IsNullOrWhiteSpace(merchantTransactionId))
                {
                    var payment = await _paymentRepository.GetPaymentByMerchantTransactionIdAsync(merchantTransactionId);
                    if (payment != null)
                    {
                        // Log the callback
                        await _paymentRepository.CreatePaymentLogAsync(new Models.PaymentLog
                        {
                            PaymentId = payment.Id,
                            Action = "CancelCallback",
                            PreviousStatus = payment.Status,
                            NewStatus = "Cancelled",
                            ResponseData = $"TransactionId: {transactionId}, MerchantTransactionId: {merchantTransactionId}",
                            IpAddress = GetClientIpAddress(),
                            CreatedAt = DateTime.UtcNow
                        });

                        // Update payment status
                        await _paymentRepository.UpdatePaymentStatusAsync(payment.Id, "Cancelled", 
                            errorCode: "USER_CANCELLED", 
                            errorMessage: "Payment was cancelled by user");
                    }
                }

                string frontendUrl = _configuration["EPS:FrontendUrl"] ?? "http://localhost:3002";
                return Redirect($"{frontendUrl}/payment/cancelled?merchantTransactionId={merchantTransactionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling payment cancel callback");
                string frontendUrl = _configuration["EPS:FrontendUrl"] ?? "http://localhost:3002";
                return Redirect($"{frontendUrl}/checkout");
            }
        }

        /// <summary>
        /// Get payment history for authenticated user
        /// </summary>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetPaymentHistory([FromQuery] int limit = 20, [FromQuery] int offset = 0)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing authentication token" });
                }

                var payments = await _paymentRepository.GetUserPaymentsAsync(userId.Value, limit, offset);

                return Ok(new
                {
                    payments = payments.Select(p => new
                    {
                        id = p.Id,
                        customerOrderId = p.CustomerOrderId,
                        merchantTransactionId = p.MerchantTransactionId,
                        amount = p.Amount,
                        currency = p.Currency,
                        status = p.Status,
                        productName = p.ProductName,
                        paymentMethod = p.PaymentMethod,
                        createdAt = p.CreatedAt,
                        completedAt = p.CompletedAt
                    }),
                    limit = limit,
                    offset = offset
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment history");
                return StatusCode(500, new { message = "An error occurred while fetching payment history" });
            }
        }

        /// <summary>
        /// Get payment statistics for authenticated user
        /// </summary>
        [HttpGet("statistics")]
        [Authorize]
        public async Task<IActionResult> GetPaymentStatistics()
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing authentication token" });
                }

                var stats = await _paymentRepository.GetUserPaymentStatisticsAsync(userId.Value);

                return Ok(new
                {
                    totalTransactions = stats.TotalTransactions,
                    successCount = stats.SuccessCount,
                    failedCount = stats.FailedCount,
                    pendingCount = stats.PendingCount,
                    cancelledCount = stats.CancelledCount,
                    expiredCount = stats.ExpiredCount,
                    totalRevenue = stats.TotalRevenue,
                    averageTransactionAmount = stats.AverageTransactionAmount,
                    successRate = stats.SuccessRate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment statistics");
                return StatusCode(500, new { message = "An error occurred while fetching payment statistics" });
            }
        }

        /// <summary>
        /// Get payment details by merchant transaction ID
        /// </summary>
        [HttpGet("details/{merchantTransactionId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentDetails(string merchantTransactionId)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (!userId.HasValue)
                {
                    return Unauthorized(new { message = "Invalid or missing authentication token" });
                }

                var payment = await _paymentRepository.GetPaymentByMerchantTransactionIdAsync(merchantTransactionId);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment transaction not found" });
                }

                if (payment.UserId != userId.Value)
                {
                    return Forbid();
                }

                var metadata = await _paymentRepository.GetPaymentMetadataAsync(payment.Id);
                var logs = await _paymentRepository.GetPaymentLogsAsync(payment.Id);

                return Ok(new
                {
                    payment = new
                    {
                        id = payment.Id,
                        customerOrderId = payment.CustomerOrderId,
                        merchantTransactionId = payment.MerchantTransactionId,
                        epsTransactionId = payment.EpsTransactionId,
                        amount = payment.Amount,
                        currency = payment.Currency,
                        status = payment.Status,
                        paymentMethod = payment.PaymentMethod,
                        productName = payment.ProductName,
                        productProfile = payment.ProductProfile,
                        productCategory = payment.ProductCategory,
                        customerName = payment.CustomerName,
                        customerEmail = payment.CustomerEmail,
                        customerPhone = payment.CustomerPhone,
                        verifiedAt = payment.VerifiedAt,
                        createdAt = payment.CreatedAt,
                        completedAt = payment.CompletedAt,
                        expiresAt = payment.ExpiresAt
                    },
                    metadata = metadata != null ? new
                    {
                        shipmentName = metadata.ShipmentName,
                        shipmentAddress = metadata.ShipmentAddress,
                        shippingMethod = metadata.ShippingMethod,
                        valueA = metadata.ValueA,
                        valueB = metadata.ValueB,
                        valueC = metadata.ValueC,
                        valueD = metadata.ValueD
                    } : null,
                    logs = logs.Select(l => new
                    {
                        action = l.Action,
                        previousStatus = l.PreviousStatus,
                        newStatus = l.NewStatus,
                        errorMessage = l.ErrorMessage,
                        createdAt = l.CreatedAt
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment details");
                return StatusCode(500, new { message = "An error occurred while fetching payment details" });
            }
        }
    }
}
