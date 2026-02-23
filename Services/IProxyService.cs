namespace real_proxy_api.Services
{
    public interface IProxyService
    {
        Task<(bool Success, string Message, string? ResidentialProxyKey, object? Data)> CreateEvomiSubUserAsync(string username, string email, decimal balance);
        Task<(bool Success, string Message, object? Data)> GiveBalanceAsync(string username, decimal balance);
    }
}
