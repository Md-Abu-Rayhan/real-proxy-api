using System.Net.Http.Json;
using real_proxy_api.DTOs;

namespace real_proxy_api.Services
{
    public class LocationService : ILocationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public LocationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<LocationResponse> GetRegionsAsync()
        {
            var token = "1fba7889-4be8-49fa-a34d-1551a96d2e7d";
            var key = "F692CNNBuDSWoNUU";
            var url = $"https://docapi.922proxy.com/api/general/regions?token={token}&key={key}&proxy_type=isp";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<LocationResponse>(url);
                return response ?? new LocationResponse { Code = "500", Msg = "Empty response from proxy API" };
            }
            catch (Exception ex)
            {
                return new LocationResponse { Code = "500", Msg = ex.Message };
            }
        }

        public async Task<StateResponse> GetStatesAsync(string countryCode)
        {
            var token = "1fba7889-4be8-49fa-a34d-1551a96d2e7d";
            var key = "F692CNNBuDSWoNUU";
            var url = $"https://docapi.922proxy.com/api/general/states?token={token}&key={key}&country_code={countryCode}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<StateResponse>(url);
                return response ?? new StateResponse { Code = "500", Msg = "Empty response from proxy API" };
            }
            catch (Exception ex)
            {
                return new StateResponse { Code = "500", Msg = ex.Message };
            }
        }

        public async Task<CityResponse> GetCitiesAsync(string countryCode, string stateCode)
        {
            var token = "1fba7889-4be8-49fa-a34d-1551a96d2e7d";
            var key = "F692CNNBuDSWoNUU";
            var url = $"https://docapi.922proxy.com/api/general/citys?token={token}&key={key}&country_code={countryCode}&state_code={stateCode}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<CityResponse>(url);
                return response ?? new CityResponse { Code = "500", Msg = "Empty response from proxy API" };
            }
            catch (Exception ex)
            {
                return new CityResponse { Code = "500", Msg = ex.Message };
            }
        }

        public async Task<AsnResponse> GetAsnsAsync(string countryCode)
        {
            var token = "1fba7889-4be8-49fa-a34d-1551a96d2e7d";
            var key = "F692CNNBuDSWoNUU";
            var url = $"https://docapi.922proxy.com/api/general/asn?token={token}&key={key}&country_code={countryCode}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<AsnResponse>(url);
                return response ?? new AsnResponse { Code = "500", Msg = "Empty response from proxy API" };
            }
            catch (Exception ex)
            {
                return new AsnResponse { Code = "500", Msg = ex.Message };
            }
        }
    }
}
