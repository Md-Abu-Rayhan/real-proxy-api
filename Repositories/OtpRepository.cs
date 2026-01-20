using Dapper;
using MySqlConnector;
using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public class OtpRepository : IOtpRepository
    {
        private readonly MySqlConnection _connection;

        public OtpRepository(MySqlConnection connection)
        {
            _connection = connection;
        }

        public async Task CreateOtpAsync(string email, string otpCode)
        {
            // Delete any existing OTPs for this email first
            await DeleteOtpAsync(email);

            var sql = @"INSERT INTO otps (Email, Otp, Timestamp) 
                        VALUES (@Email, @Otp, @Timestamp)";

            await _connection.ExecuteAsync(sql, new
            {
                Email = email,
                Otp = otpCode,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task<Otp?> GetValidOtpAsync(string email, string otpCode)
        {
            var sql = @"SELECT Id, Email, Otp AS OtpCode, Timestamp 
                        FROM otps 
                        WHERE Email = @Email AND Otp = @Otp";

            var otp = await _connection.QueryFirstOrDefaultAsync<Otp>(sql, new
            {
                Email = email,
                Otp = otpCode
            });

            if (otp == null)
                return null;

            // Check if OTP is still valid (within 3 minutes)
            var expirationTime = otp.Timestamp.AddMinutes(3);
            if (DateTime.UtcNow > expirationTime)
                return null;

            return otp;
        }

        public async Task DeleteOtpAsync(string email)
        {
            var sql = "DELETE FROM otps WHERE Email = @Email";
            await _connection.ExecuteAsync(sql, new { Email = email });
        }
    }
}
