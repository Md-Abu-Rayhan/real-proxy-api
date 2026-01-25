using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class AsnResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = default!;

        [JsonPropertyName("data")]
        public List<AsnData> Data { get; set; } = default!;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = default!;
    }

    public class AsnData
    {
        [JsonPropertyName("asn_code")]
        public string AsnCode { get; set; } = default!;

        [JsonPropertyName("asn_name")]
        public string AsnName { get; set; } = default!;

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; } = default!;
    }
}
