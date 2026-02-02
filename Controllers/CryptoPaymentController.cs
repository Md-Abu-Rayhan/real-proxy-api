using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using real_proxy_api.DTOs;
using real_proxy_api.Services;
using System.Text.Json;

namespace real_proxy_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CryptoPaymentController : ControllerBase
    {
        private readonly ICryptoPaymentService _cryptoPaymentService;
        private readonly ILogger<CryptoPaymentController> _logger;

        public CryptoPaymentController(ICryptoPaymentService cryptoPaymentService, ILogger<CryptoPaymentController> logger)
        {
            _cryptoPaymentService = cryptoPaymentService;
            _logger = logger;
        }

        [HttpPost("initialize")]
        //[Authorize]
        public async Task<IActionResult> InitializePayment([FromBody] CryptoPaymentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OrderId))
                {
                    return BadRequest(new { message = "OrderId is required" });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be greater than 0" });
                }

                var paymentUrl = await _cryptoPaymentService.CreateOneTimePaymentAsync(request);

                return Ok(new
                {
                    success = true,
                    paymentUrl = paymentUrl,
                    message = "MixPay payment initialized successfully."
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
            // Placeholder for MixPay callback handling
            _logger.LogInformation("MixPay callback received: {Data}", callbackData.GetRawText());
            
            // In a real scenario, you would verify the signature and update the order status
            
            return Ok(new { success = true });
        }
    }
}
