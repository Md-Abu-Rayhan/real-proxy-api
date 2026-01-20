using Microsoft.AspNetCore.Mvc;
using real_proxy_api.DTOs;
using real_proxy_api.Services;

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
    }

}
