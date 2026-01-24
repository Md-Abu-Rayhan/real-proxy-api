using System.Text.Json.Serialization;

namespace real_proxy_api.DTOs
{
    public class StateResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("data")]
        public List<StateData> Data { get; set; }

        [JsonPropertyName("msg")]
        public string Msg { get; set; }
    }

    public class StateData
    {
        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("state_code")]
        public string StateCode { get; set; }

        [JsonPropertyName("state_name")]
        public string StateName { get; set; }
    }
}
