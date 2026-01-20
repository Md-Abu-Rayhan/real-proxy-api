using real_proxy_api.DTOs;
using real_proxy_api.Repositories;

namespace real_proxy_api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IOtpRepository _otpRepository;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IJwtService jwtService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(RegisterRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return (false, "Email and Password are required.");
            }

            // Check if user already exists
            var userExists = await _userRepository.UserExistsAsync(request.Email);
            if (userExists)
            {
                return (false, "User already exists");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create user
            await _userRepository.CreateUserAsync(request.Email, passwordHash, request.InvitationCode);

            return (true, "User registered successfully");
        }

        public async Task<(bool Success, string Message, string? Token)> LoginAsync(LoginRequest request)
        {
            var email = request.Email?.Trim();
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
            {
                return (false, "Email and Password are required.", null);
            }

            // Get user
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return (false, "Invalid credentials", null);
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return (false, "Invalid credentials", null);
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Email);

            return (true, "Login successful", token);
        }

        public async Task<(bool Success, string Message)> SendOtpAsync(ForgetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return (false, "Email is required.");
            }

            // Check if user exists
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return (false, "User not found.");
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();

            // Store OTP in database
            await _otpRepository.CreateOtpAsync(request.Email, otp);

            // Send OTP via email
            try
            {
                await _emailService.SendOtpEmailAsync(request.Email, otp);
                return (true, "OTP sent to your email successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to send email: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp) || string.IsNullOrEmpty(request.NewPassword))
            {
                return (false, "Email, OTP, and NewPassword are required.");
            }

            // Validate OTP
            var validOtp = await _otpRepository.GetValidOtpAsync(request.Email, request.Otp);
            if (validOtp == null)
            {
                return (false, "Invalid or expired OTP.");
            }

            // Hash new password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            // Update password
            await _userRepository.UpdatePasswordAsync(request.Email, passwordHash);

            // Delete used OTP
            await _otpRepository.DeleteOtpAsync(request.Email);

            return (true, "Password reset successfully.");
        }
    }
}
