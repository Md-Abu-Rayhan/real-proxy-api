using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using real_proxy_api.DTOs;
using real_proxy_api.Services;
using System.Security.Claims;

namespace real_proxy_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return result.Message.Contains("exists") ? Conflict(result.Message) : BadRequest(result.Message);
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result.Message);
            }

            return Ok(new { token = result.Token });
        }



        [HttpPost("forget-password/send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] ForgetPasswordRequest request)
        {
            var result = await _authService.SendOtpAsync(request);

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result.Message);
                if (result.Message.Contains("Failed to send"))
                    return StatusCode(500, result.Message);
                return BadRequest(result.Message);
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("forget-password/reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);

            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(new { message = result.Message });
        }

        [HttpPost("add-sub-account")]
        public async Task<IActionResult> AddSubAccount([FromBody] AddSubAccountRequest request)
        {
            var result = await _authService.AddSubAccountAsync(request);

            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                    return NotFound(result.Message);
                if (result.Message.Contains("already has"))
                    return Conflict(new { message = result.Message });
                return BadRequest(result.Message);
            }

            return Ok(new 
            { 
                message = result.Message,
                data = result.Data
            });
        }

        [Authorize]
        [HttpGet("get-proxy-info")]
        public async Task<IActionResult> GetProxyInfo()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                // Token is valid (Authorize), but missing expected email claim.
                return Unauthorized("Email claim is missing from token");
            }

            var result = await _authService.GetProxyInfoAsync(email);

            if (!result.Success)
            {
                return NotFound(result.Message);
            }

            return Ok(new
            {
                proxyAccount = result.ProxyAccount,
                proxyPassword = result.ProxyPassword
            });
        }
    }

}
