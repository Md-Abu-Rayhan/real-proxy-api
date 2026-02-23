using System.Collections.Generic;

namespace real_proxy_api.Models
{
    public class ProxySettingsResponse
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public long Timestamp { get; set; }
        public ProxyData Data { get; set; }
    }

    public class ProxyData
    {
        public ResidentialSettings Residential { get; set; }
    }

    public class ResidentialSettings
    {
        public Dictionary<string, string> Countries { get; set; }
        public CitiesSettings Cities { get; set; }
        public List<string> Isp { get; set; }
        public Dictionary<string, List<string>> Continents { get; set; }
    }

    public class CitiesSettings
    {
        public List<CityData> Data { get; set; }
    }

    public class CityData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
    }
}
