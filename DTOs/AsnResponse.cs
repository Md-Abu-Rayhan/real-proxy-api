using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class AsnResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("data")]
        public List<AsnData> Data { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }
    }

    public class AsnData
    {
        [JsonPropertyName("asn_code")]
        public string AsnCode { get; set; }

        [JsonPropertyName("asn_name")]
        public string AsnName { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }
    }
}
