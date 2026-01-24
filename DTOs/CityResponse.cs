using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class CityResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("data")]
        public List<CityData> Data { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }
    }

    public class CityData
    {
        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("state_code")]
        public string StateCode { get; set; }

        [JsonPropertyName("city_code")]
        public string CityCode { get; set; }

        [JsonPropertyName("city_name")]
        public string CityName { get; set; }
    }
}
