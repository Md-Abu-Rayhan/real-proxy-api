using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class LocationResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("data")]
        public List<LocationData> Data { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }
    }

    public class LocationData
    {
        [JsonPropertyName("country_name")]
        public string CountryName { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("domain")]
        public List<DomainInfo> Domain { get; set; }

        [JsonPropertyName("whitelists")]
        public object Whitelists { get; set; }
    }

    public class DomainInfo
    {
        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("port")]
        public string Port { get; set; }
    }
}
