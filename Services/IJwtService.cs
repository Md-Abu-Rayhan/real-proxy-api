namespace real_proxy_api.Services
{
    public interface IJwtService
    {
        string GenerateToken(string email, int userId);
    }
}
