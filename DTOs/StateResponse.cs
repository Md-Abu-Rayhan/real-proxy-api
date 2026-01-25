using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class StateResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = default!;

        [JsonPropertyName("data")]
        public List<StateData> Data { get; set; } = default!;

        [JsonPropertyName("msg")]
        public string Msg { get; set; } = default!;
    }

    public class StateData
    {
        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; } = default!;

        [JsonPropertyName("state_code")]
        public string StateCode { get; set; } = default!;

        [JsonPropertyName("state_name")]
        public string StateName { get; set; } = default!;
    }
}
