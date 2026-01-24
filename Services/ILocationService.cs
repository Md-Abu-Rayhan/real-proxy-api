using real_proxy_api.DTOs;

namespace real_proxy_api.Services
{
    public interface ILocationService
    {
        Task<LocationResponse> GetRegionsAsync();
        Task<StateResponse> GetStatesAsync(string countryCode);
        Task<CityResponse> GetCitiesAsync(string countryCode, string stateCode);
        Task<AsnResponse> GetAsnsAsync(string countryCode);
    }
}
