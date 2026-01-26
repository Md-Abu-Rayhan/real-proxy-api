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
        private readonly IProxyService _proxyService;

        public AuthService(
            IUserRepository userRepository,
            IOtpRepository otpRepository,
            IJwtService jwtService,
            IEmailService emailService,
            IProxyService proxyService)
        {
            _userRepository = userRepository;
            _otpRepository = otpRepository;
            _jwtService = jwtService;
            _emailService = emailService;
            _proxyService = proxyService;
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

            // Generate unique proxy account and password
            string proxyAccount;
            do
            {
                proxyAccount = GenerateUniqueAccount();
            } while (await _userRepository.ProxyAccountExistsAsync(proxyAccount));

            var proxyPassword = GenerateUniquePassword();

            // Call the 922proxy API to create sub-account
            var proxyResult = await _proxyService.CreateSubAccountAsync(
                proxyAccount,
                proxyPassword,
                proxyType: 1,
                remark: request.Email,
                traffic: 10,
                trafficUnit: "MB",
                bandwidth: 0,
                hostname: 0,
                status: 1);

            if (!proxyResult.Success)
            {
                return (false, $"Failed to create proxy account: {proxyResult.Message}");
            }

            // Create user with all fields at once
            await _userRepository.CreateUserWithProxyAsync(request.Email, passwordHash, request.InvitationCode, proxyAccount, proxyPassword);

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

            // Generate JWT token with userId
            var token = _jwtService.GenerateToken(user.Email, user.Id);

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

        public async Task<(bool Success, string Message, string? ProxyAccount, string? ProxyPassword)> AddSubAccountAsync(AddSubAccountRequest request)
        {
            if (string.IsNullOrEmpty(request.Email))
            {
                return (false, "Email is required.", null, null);
            }

            // Check if user exists
            var user = await _userRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return (false, "User not found. Please register first.", null, null);
            }

            // Check if user already has a proxy account
            if (!string.IsNullOrEmpty(user.ProxyAccount))
            {
                return (false, "User already has a proxy sub-account.", user.ProxyAccount, null);
            }

            // Generate unique account name and password
            string proxyAccount;
            do
            {
                proxyAccount = GenerateUniqueAccount();
            } while (await _userRepository.ProxyAccountExistsAsync(proxyAccount));

            var proxyPassword = GenerateUniquePassword();

            // Call the 922proxy API to create sub-account
            var result = await _proxyService.CreateSubAccountAsync(
                proxyAccount,
                proxyPassword,
                request.ProxyType,
                request.Remark,
                request.Traffic,
                request.TrafficUnit,
                request.Bandwidth,
                request.Hostname,
                request.Status);

            if (!result.Success)
            {
                return (false, result.Message, null, null);
            }

            // Update user with proxy account info
            await _userRepository.UpdateProxyAccountAsync(request.Email, proxyAccount, proxyPassword);

            return (true, "Sub-account created successfully.", proxyAccount, proxyPassword);
        }

        private static string GenerateUniqueAccount()
        {
            // Generate account like: user_xxxxxxxx (8 random alphanumeric characters)
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var randomPart = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"user_{randomPart}";
        }

        private static string GenerateUniquePassword()
        {
            // Generate a strong 12-character password
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<(bool Success, string Message, string? ProxyAccount, string? ProxyPassword)> GetProxyInfoAsync(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return (false, "User not found", null, null);
            }

            if (string.IsNullOrEmpty(user.ProxyAccount))
            {
                return (false, "No proxy account found for this user", null, null);
            }

            return (true, "Success", user.ProxyAccount, user.ProxyPassword);
        }
    }
}
