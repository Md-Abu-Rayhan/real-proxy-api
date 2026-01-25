using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class LocationResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = default!;

        [JsonPropertyName("data")]
        public List<LocationData> Data { get; set; } = default!;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = default!;
    }

    public class LocationData
    {
        [JsonPropertyName("country_name")]
        public string CountryName { get; set; } = default!;

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; } = default!;

        [JsonPropertyName("domain")]
        public List<DomainInfo> Domain { get; set; } = default!;

        [JsonPropertyName("whitelists")]
        public object Whitelists { get; set; } = default!;
    }

    public class DomainInfo
    {
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = default!;

        [JsonPropertyName("port")]
        public string Port { get; set; } = default!;
    }
}
