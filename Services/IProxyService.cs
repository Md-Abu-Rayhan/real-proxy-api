namespace real_proxy_api.Services
{
    public interface IProxyService
    {
        Task<(bool Success, string Message, string? Account, string? Password)> CreateSubAccountAsync(
            string account,
            string password,
            int proxyType,
            string? remark,
            int traffic,
            string trafficUnit,
            int bandwidth,
            int hostname,
            int status);
    }
}
