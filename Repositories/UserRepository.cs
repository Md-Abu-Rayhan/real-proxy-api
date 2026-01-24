using Dapper;
using MySqlConnector;
using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MySqlConnection _connection;

        public UserRepository(MySqlConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var sql = "SELECT * FROM users WHERE Email = @Email";
            return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            var sql = "SELECT COUNT(1) FROM users WHERE Email = @Email";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { Email = email });
            return count > 0;
        }

        public async Task<int> CreateUserAsync(string email, string passwordHash, string? invitationCode)
        {
            var sql = @"INSERT INTO users (Email, PasswordHash, InvitationCode) 
                        VALUES (@Email, @PasswordHash, @InvitationCode);
                        SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = email,
                PasswordHash = passwordHash,
                InvitationCode = invitationCode ?? string.Empty
            });
        }

        public async Task<int> CreateUserWithProxyAsync(string email, string passwordHash, string? invitationCode, string proxyAccount, string proxyPassword)
        {
            var sql = @"INSERT INTO users (Email, PasswordHash, InvitationCode, ProxyAccount, ProxyPassword) 
                        VALUES (@Email, @PasswordHash, @InvitationCode, @ProxyAccount, @ProxyPassword);
                        SELECT LAST_INSERT_ID();";

            return await _connection.ExecuteScalarAsync<int>(sql, new
            {
                Email = email,
                PasswordHash = passwordHash,
                InvitationCode = invitationCode ?? string.Empty,
                ProxyAccount = proxyAccount,
                ProxyPassword = proxyPassword
            });
        }

        public async Task UpdatePasswordAsync(string email, string passwordHash)
        {
            var sql = "UPDATE users SET PasswordHash = @PasswordHash WHERE Email = @Email";
            await _connection.ExecuteAsync(sql, new
            {
                Email = email,
                PasswordHash = passwordHash
            });
        }

        public async Task UpdateUserAsync(string email, string passwordHash, string? invitationCode)
        {
            var sql = "UPDATE users SET PasswordHash = @PasswordHash, InvitationCode = @InvitationCode WHERE Email = @Email";
            await _connection.ExecuteAsync(sql, new
            {
                Email = email,
                PasswordHash = passwordHash,
                InvitationCode = invitationCode ?? string.Empty
            });
        }

        public async Task UpdateProxyAccountAsync(string email, string proxyAccount, string proxyPassword)
        {
            var sql = "UPDATE users SET ProxyAccount = @ProxyAccount, ProxyPassword = @ProxyPassword WHERE Email = @Email";
            await _connection.ExecuteAsync(sql, new
            {
                Email = email,
                ProxyAccount = proxyAccount,
                ProxyPassword = proxyPassword
            });
        }

        public async Task<bool> ProxyAccountExistsAsync(string proxyAccount)
        {
            var sql = "SELECT COUNT(1) FROM users WHERE ProxyAccount = @ProxyAccount";
            var count = await _connection.ExecuteScalarAsync<int>(sql, new { ProxyAccount = proxyAccount });
            return count > 0;
        }
    }
}
