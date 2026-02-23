using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace real_proxy_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetProxySettings()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://reseller.evomi.com/v2/reseller/proxy_settings");
                request.Headers.Add("X-API-KEY", "xrLkWmoX8AFx6G72ipvU"); // You might want to move this to appsettings.json later

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Error calling Evomi API: {response.ReasonPhrase}");
                }

                var content = await response.Content.ReadAsStringAsync();
                
                var proxySettingsResponse = System.Text.Json.Nodes.JsonNode.Parse(content);

                var residentialData = proxySettingsResponse?["data"]?["residential"];

                if (residentialData == null)
                {
                    return NotFound("Residential proxy settings not found in Evomi response.");
                }

                var filteredResponse = new
                {
                    status = 200,
                    message = "",
                    timestamp = proxySettingsResponse?["timestamp"]?.GetValue<long>() ?? System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    data = new
                    {
                        residential = new
                        {
                            countries = residentialData["countries"],
                            cities = residentialData["cities"],
                            regions = residentialData["regions"],
                            isp = residentialData["isp"],
                            continents = residentialData["continents"]
                        }
                    }
                };

                return Ok(filteredResponse);
            }
            catch (Exception ex)
            {
                // In production you would log the exception here
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
