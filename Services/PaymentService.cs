using real_proxy_api.DTOs;
using real_proxy_api.Models;
using real_proxy_api.Repositories;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace real_proxy_api.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IMemoryCache _memoryCache;
        private const string TokenCacheKey = "EpsAuthToken";

        public PaymentService(HttpClient httpClient, IConfiguration configuration, 
            ILogger<PaymentService> logger, IPaymentRepository paymentRepository, IMemoryCache memoryCache)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _paymentRepository = paymentRepository;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Generate HMACSHA512 hash as per EPS specification
        /// Step 1: Encode Hash Key using UTF8
        /// Step 2: Create HMACSHA512 using encoded data
        /// Step 3: Compute Hash using created hmac and data
        /// Step 4: Return Base64 string of Hash
        /// </summary>
        public string GenerateHash(string data, string hashKey)
        {
            try
            {
                // Step 1: Encode Hash Key using UTF8
                var encodedKey = Encoding.UTF8.GetBytes(hashKey);

                // Step 2 & 3: Create HMACSHA512 and compute hash
                using (var hmac = new HMACSHA512(encodedKey))
                {
                    var dataBytes = Encoding.UTF8.GetBytes(data);
                    var hashBytes = hmac.ComputeHash(dataBytes);

                    // Step 4: Return Base64 string
                    return Convert.ToBase64String(hashBytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating hash for data: {Data}", data);
                throw;
            }
        }

        /// <summary>
        /// Get authentication token from EPS Payment Gateway (API No. 01)
        /// </summary>
        public async Task<GetTokenResponse> GetTokenAsync()
        {
            try
            {
                // Check if we have a valid cached token
                if (_memoryCache.TryGetValue(TokenCacheKey, out GetTokenResponse? cachedResponse) && cachedResponse != null)
                {
                    _logger.LogInformation("Using cached EPS token (expires: {ExpireDate})", cachedResponse.ExpireDate);
                    return cachedResponse;
                }

                var epsSettings = _configuration.GetSection("EPS");
                var userName = epsSettings["UserName"] ?? throw new InvalidOperationException("EPS UserName not configured");
                var password = epsSettings["Password"] ?? throw new InvalidOperationException("EPS Password not configured");
                var hashKey = epsSettings["HashKey"] ?? throw new InvalidOperationException("EPS HashKey not configured");
                var baseUrl = epsSettings["BaseUrl"] ?? throw new InvalidOperationException("EPS BaseUrl not configured");

                // Generate x-hash using username
                var xHash = GenerateHash(userName, hashKey);

                var request = new GetTokenRequest
                {
                    UserName = userName,
                    Password = password
                };

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/Auth/GetToken")
                {
                    Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
                };

                // Add x-hash header
                httpRequest.Headers.Add("x-hash", xHash);

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("EPS GetToken failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);
                    return new GetTokenResponse
                    {
                        ErrorMessage = $"Failed to get token: {response.StatusCode}",
                        ErrorCode = response.StatusCode.ToString()
                    };
                }

                var tokenResponse = JsonSerializer.Deserialize<GetTokenResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    // Calculate expiration
                    var expiryTime = TimeSpan.FromHours(1); // Default
                    if (DateTime.TryParse(tokenResponse.ExpireDate, out var expireDate))
                    {
                         // Expires 5 minutes before actual expiry to be safe, but ensure it's at least 1 minute
                         var timeUntilExpiry = expireDate - DateTime.Now;
                         if (timeUntilExpiry.TotalMinutes > 5)
                         {
                             expiryTime = timeUntilExpiry.Add(TimeSpan.FromMinutes(-5));
                         }
                         else
                         {
                             expiryTime = TimeSpan.FromMinutes(1);
                         }
                    }

                    // Cache the token
                    _memoryCache.Set(TokenCacheKey, tokenResponse, expiryTime);
                    _logger.LogInformation("Cached new EPS token for {ExpiryMinutes} minutes (expires: {ExpireDate})", 
                        expiryTime.TotalMinutes, tokenResponse.ExpireDate);
                }

                return tokenResponse ?? new GetTokenResponse
                {
                    ErrorMessage = "Failed to parse token response",
                    ErrorCode = "PARSE_ERROR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting EPS token");
                return new GetTokenResponse
                {
                    ErrorMessage = ex.Message,
                    ErrorCode = "EXCEPTION"
                };
            }
        }

        /// <summary>
        /// Initialize payment and get redirect URL (API No. 02)
        /// </summary>
        public async Task<InitializePaymentResponse> InitializePaymentAsync(InitializePaymentRequest request, int userId, string? ipAddress = null, string? userAgent = null)
        {
            try
            {
                // Start token fetch early (can run in parallel with validation)
                var tokenTask = GetTokenAsync();

                // Check for duplicate transactions
                if (await _paymentRepository.MerchantTransactionIdExistsAsync(request.MerchantTransactionId))
                {
                    _logger.LogWarning("Duplicate merchant transaction ID: {MerchantTransactionId}", request.MerchantTransactionId);
                    return new InitializePaymentResponse
                    {
                        ErrorMessage = "This transaction ID already exists. Please use a unique transaction ID.",
                        ErrorCode = "DUPLICATE_TRANSACTION_ID"
                    };
                }

                if (await _paymentRepository.CustomerOrderIdExistsAsync(request.CustomerOrderId))
                {
                    _logger.LogWarning("Duplicate customer order ID: {CustomerOrderId}", request.CustomerOrderId);
                    return new InitializePaymentResponse
                    {
                        ErrorMessage = "This order ID already exists. Please use a unique order ID.",
                        ErrorCode = "DUPLICATE_ORDER_ID"
                    };
                }

                // Wait for token (should be ready by now or use cached)
                var tokenResponse = await tokenTask;
                if (string.IsNullOrEmpty(tokenResponse.Token))
                {
                    return new InitializePaymentResponse
                    {
                        ErrorMessage = tokenResponse.ErrorMessage ?? "Failed to get authentication token",
                        ErrorCode = tokenResponse.ErrorCode ?? "TOKEN_ERROR"
                    };
                }

                var epsSettings = _configuration.GetSection("EPS");
                var hashKey = epsSettings["HashKey"] ?? throw new InvalidOperationException("EPS HashKey not configured");
                var baseUrl = epsSettings["BaseUrl"] ?? throw new InvalidOperationException("EPS BaseUrl not configured");

                // Set merchant and store IDs from configuration if not provided
                if (string.IsNullOrEmpty(request.MerchantId))
                {
                    request.MerchantId = epsSettings["MerchantId"] ?? throw new InvalidOperationException("EPS MerchantId not configured");
                }
                if (string.IsNullOrEmpty(request.StoreId))
                {
                    request.StoreId = epsSettings["StoreId"] ?? throw new InvalidOperationException("EPS StoreId not configured");
                }

                // Generate verification hash for security
                var verificationData = $"{request.MerchantTransactionId}:{request.TotalAmount}:{userId}:{DateTime.UtcNow:yyyyMMddHHmmss}";
                var verificationHash = GenerateHash(verificationData, hashKey);

                // Create payment record in database
                var payment = new Payment
                {
                    UserId = userId,
                    CustomerOrderId = request.CustomerOrderId,
                    MerchantTransactionId = request.MerchantTransactionId,
                    Amount = request.TotalAmount,
                    Currency = "BDT",
                    TransactionTypeId = request.TransactionTypeId,
                    Status = "Pending",
                    ProductName = request.ProductName,
                    ProductProfile = request.ProductProfile,
                    ProductCategory = request.ProductCategory,
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    CustomerAddress = request.CustomerAddress,
                    CustomerCity = request.CustomerCity,
                    CustomerState = request.CustomerState,
                    CustomerPostcode = request.CustomerPostcode,
                    CustomerCountry = request.CustomerCountry,
                    SuccessUrl = request.SuccessUrl,
                    FailUrl = request.FailUrl,
                    CancelUrl = request.CancelUrl,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    VerificationHash = verificationHash,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24) // Payment link expires in 24 hours
                };

                // Create initial log
                var initialLog = new PaymentLog
                {
                    Action = "Initialize",
                    NewStatus = "Pending",
                    RequestData = JsonSerializer.Serialize(request),
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to database in transaction
                var paymentId = await _paymentRepository.CreatePaymentWithLogAsync(payment, initialLog);

                var xHash = GenerateHash(request.MerchantTransactionId, hashKey);

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };

                var jsonPayload = JsonSerializer.Serialize(request, jsonOptions);
                _logger.LogInformation("EPS InitializeRequest Payload: {Payload}", jsonPayload);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/EPSEngine/InitializeEPS")
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                // Add headers
                httpRequest.Headers.Add("x-hash", xHash);
                httpRequest.Headers.Add("Authorization", $"Bearer {tokenResponse.Token}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("EPS InitializePayment failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);
                    
                    // Log the error
                    await _paymentRepository.CreatePaymentLogAsync(new PaymentLog
                    {
                        PaymentId = paymentId,
                        Action = "InitializeError",
                        ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Update payment status
                    await _paymentRepository.UpdatePaymentStatusAsync(paymentId, "Failed", 
                        errorCode: response.StatusCode.ToString(), 
                        errorMessage: $"EPS Error: {responseContent}");

                    return new InitializePaymentResponse
                    {
                        ErrorMessage = $"Failed to initialize payment: {response.StatusCode}. Content: {responseContent}",
                        ErrorCode = response.StatusCode.ToString()
                    };
                }

                var initResponse = JsonSerializer.Deserialize<InitializePaymentResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (initResponse != null && !string.IsNullOrEmpty(initResponse.TransactionId))
                {
                    // Update payment with EPS transaction ID
                    await _paymentRepository.UpdatePaymentStatusAsync(paymentId, "Pending", 
                        epsTransactionId: initResponse.TransactionId);

                    // Log successful initialization
                    await _paymentRepository.CreatePaymentLogAsync(new PaymentLog
                    {
                        PaymentId = paymentId,
                        Action = "InitializeSuccess",
                        ResponseData = responseContent,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                return initResponse ?? new InitializePaymentResponse
                {
                    ErrorMessage = "Failed to parse initialize payment response",
                    ErrorCode = "PARSE_ERROR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment");
                return new InitializePaymentResponse
                {
                    ErrorMessage = ex.Message,
                    ErrorCode = "EXCEPTION"
                };
            }
        }

        /// <summary>
        /// Verify transaction status (API No. 03)
        /// </summary>
        public async Task<VerifyTransactionResponse> VerifyTransactionAsync(string merchantTransactionId, string? ipAddress = null)
        {
            try
            {
                // Get payment from database first
                var payment = await _paymentRepository.GetPaymentByMerchantTransactionIdAsync(merchantTransactionId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment not found for merchant transaction ID: {MerchantTransactionId}", merchantTransactionId);
                    return new VerifyTransactionResponse
                    {
                        ErrorMessage = "Payment transaction not found",
                        ErrorCode = "TRANSACTION_NOT_FOUND"
                    };
                }

                // Increment verification attempts
                await _paymentRepository.IncrementVerificationAttemptsAsync(payment.Id);

                // If already verified and successful, return cached data
                if (payment.Status == "Success" && payment.VerifiedAt.HasValue)
                {
                    _logger.LogInformation("Returning cached successful payment for: {MerchantTransactionId}", merchantTransactionId);

                    return new VerifyTransactionResponse
                    {
                        MerchantTransactionId = payment.MerchantTransactionId,
                        Status = payment.Status,
                        TotalAmount = payment.Amount.ToString("F2"),
                        TransactionDate = payment.CompletedAt?.ToString("dd MMM yyyy hh:mm:ss tt"),
                        TransactionType = "Purchase",
                        FinancialEntity = payment.PaymentMethod,
                        CustomerName = payment.CustomerName,
                        CustomerEmail = payment.CustomerEmail,
                        CustomerPhone = payment.CustomerPhone,
                        CustomerAddress = payment.CustomerAddress,
                        CustomerCity = payment.CustomerCity,
                        CustomerState = payment.CustomerState,
                        CustomerPostcode = payment.CustomerPostcode,
                        CustomerCountry = payment.CustomerCountry,
                        ProductName = payment.ProductName,
                        ProductProfile = payment.ProductProfile,
                        ProductCategory = payment.ProductCategory
                    };
                }

                // Get token first
                var tokenResponse = await GetTokenAsync();
                if (string.IsNullOrEmpty(tokenResponse.Token))
                {
                    return new VerifyTransactionResponse
                    {
                        ErrorMessage = tokenResponse.ErrorMessage ?? "Failed to get authentication token",
                        ErrorCode = tokenResponse.ErrorCode ?? "TOKEN_ERROR"
                    };
                }

                var epsSettings = _configuration.GetSection("EPS");
                var hashKey = epsSettings["HashKey"] ?? throw new InvalidOperationException("EPS HashKey not configured");
                var baseUrl = epsSettings["BaseUrl"] ?? throw new InvalidOperationException("EPS BaseUrl not configured");

                // Generate x-hash using merchantTransactionId
                var xHash = GenerateHash(merchantTransactionId, hashKey);

                var httpRequest = new HttpRequestMessage(HttpMethod.Get,
                    $"{baseUrl}/v1/EPSEngine/CheckMerchantTransactionStatus?merchantTransactionId={merchantTransactionId}");

                // Add headers
                httpRequest.Headers.Add("x-hash", xHash);
                httpRequest.Headers.Add("Authorization", $"Bearer {tokenResponse.Token}");

                var response = await _httpClient.SendAsync(httpRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log verification attempt
                await _paymentRepository.CreatePaymentLogAsync(new PaymentLog
                {
                    PaymentId = payment.Id,
                    Action = "Verify",
                    PreviousStatus = payment.Status,
                    ResponseData = responseContent,
                    IpAddress = ipAddress,
                    CreatedAt = DateTime.UtcNow
                });

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("EPS VerifyTransaction failed with status {StatusCode}: {Response}", response.StatusCode, responseContent);

                    await _paymentRepository.CreatePaymentLogAsync(new PaymentLog
                    {
                        PaymentId = payment.Id,
                        Action = "VerifyError",
                        ErrorMessage = $"HTTP {response.StatusCode}: {responseContent}",
                        IpAddress = ipAddress,
                        CreatedAt = DateTime.UtcNow
                    });

                    return new VerifyTransactionResponse
                    {
                        ErrorMessage = $"Failed to verify transaction: {response.StatusCode}",
                        ErrorCode = response.StatusCode.ToString()
                    };
                }

                var verifyResponse = JsonSerializer.Deserialize<VerifyTransactionResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (verifyResponse != null && !string.IsNullOrEmpty(verifyResponse.Status))
                {
                    // Update payment status in database
                    var newStatus = verifyResponse.Status;
                    var paymentMethod = verifyResponse.FinancialEntity;

                    await _paymentRepository.MarkPaymentVerifiedAsync(payment.Id, newStatus, paymentMethod);

                    // Log status change
                    await _paymentRepository.CreatePaymentLogAsync(new PaymentLog
                    {
                        PaymentId = payment.Id,
                        Action = "StatusChange",
                        PreviousStatus = payment.Status,
                        NewStatus = newStatus,
                        ResponseData = responseContent,
                        IpAddress = ipAddress,
                        CreatedAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("Payment verified successfully. MerchantTxnId: {MerchantTransactionId}, Status: {Status}",
                        merchantTransactionId, newStatus);
                }

                return verifyResponse ?? new VerifyTransactionResponse
                {
                    ErrorMessage = "Failed to parse verify transaction response",
                    ErrorCode = "PARSE_ERROR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying transaction");
                return new VerifyTransactionResponse
                {
                    ErrorMessage = ex.Message,
                    ErrorCode = "EXCEPTION"
                };
            }
        }
        /// <summary>
        /// Initialize payment securely by looking up price on server
        /// </summary>
        public async Task<InitializePaymentResponse> InitializeSecurePaymentAsync(SecurePaymentRequest request, int userId, string? ipAddress = null, string? userAgent = null)
        {
            // 1. Define packages (Ideally these would be in a database)
            var packages = new Dictionary<string, (string Name, decimal Price)> 
            {
                { "res_10gb", ("Residential 10GB", 15.00m) },
                { "res_50gb", ("Residential 50GB", 65.00m) },
                { "res_100gb", ("Residential 100GB", 120.00m) },
                { "premium_pkg", ("Premium Package", 1000.50m) }
            };

            if (!packages.TryGetValue(request.PackageId, out var package))
            {
                return new InitializePaymentResponse
                {
                    ErrorMessage = "Invalid Package ID selected.",
                    ErrorCode = "INVALID_PACKAGE"
                };
            }

            // 2. Generate unique MerchantTransactionId
            string merchantTxnId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{new Random().Next(1000, 9999)}";

            // 3. Create the full initialization request
            var fullRequest = new InitializePaymentRequest
            {
                CustomerOrderId = request.CustomerOrderId,
                MerchantTransactionId = merchantTxnId,
                TransactionTypeId = 1, // Web
                TotalAmount = package.Price,
                ProductName = package.Name,
                ProductProfile = "general",
                ProductCategory = "Proxy",
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                CustomerAddress = request.CustomerAddress,
                CustomerCity = request.CustomerCity,
                CustomerState = request.CustomerState,
                CustomerPostcode = request.CustomerPostcode,
                CustomerCountry = request.CustomerCountry,
                // Default URLs (should ideally come from config)
                SuccessUrl = _configuration["EPS:SuccessUrl"] ?? "https://api.realproxy.net/api/Payment/callback/success",
                FailUrl = _configuration["EPS:FailUrl"] ?? "https://api.realproxy.net/api/Payment/callback/fail",
                CancelUrl = _configuration["EPS:CancelUrl"] ?? "https://api.realproxy.net/api/Payment/callback/cancel"
            };

            // 4. Call the existing initialization logic
            return await InitializePaymentAsync(fullRequest, userId, ipAddress, userAgent);
        }
    }
}
