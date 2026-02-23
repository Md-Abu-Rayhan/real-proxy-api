using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> UserExistsAsync(string email);
        Task<int> CreateUserAsync(string email, string passwordHash, string? invitationCode);
        Task<int> CreateUserWithProxyAsync(string email, string passwordHash, string? invitationCode, string proxyAccount, string proxyPassword);
        Task UpdatePasswordAsync(string email, string passwordHash);
        Task UpdateUserAsync(string email, string passwordHash, string? invitationCode);
        Task UpdateProxyAccountAsync(string email, string proxyAccount, string proxyPassword);
        Task<bool> ProxyAccountExistsAsync(string proxyAccount);
    }
}
