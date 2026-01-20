using real_proxy_api.Models;

namespace real_proxy_api.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<bool> UserExistsAsync(string email);
        Task<int> CreateUserAsync(string email, string passwordHash, string? invitationCode);
        Task UpdatePasswordAsync(string email, string passwordHash);
    }
}
