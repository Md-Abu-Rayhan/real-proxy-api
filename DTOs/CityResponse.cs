using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class CityResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = default!;

        [JsonPropertyName("data")]
        public List<CityData> Data { get; set; } = default!;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = default!;
    }

    public class CityData
    {
        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; } = default!;

        [JsonPropertyName("state_code")]
        public string StateCode { get; set; } = default!;

        [JsonPropertyName("city_code")]
        public string CityCode { get; set; } = default!;

        [JsonPropertyName("city_name")]
        public string CityName { get; set; } = default!;
    }
}
