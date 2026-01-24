using Microsoft.AspNetCore.Mvc;
using real_proxy_api.Services;

namespace real_proxy_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpGet("regions")]
        public async Task<IActionResult> GetRegions()
        {
            var result = await _locationService.GetRegionsAsync();

            if (result.Code != "200")
            {
                if (int.TryParse(result.Code, out int statusCode))
                {
                    return StatusCode(statusCode, result);
                }
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        [HttpGet("states")]
        public async Task<IActionResult> GetStates([FromQuery] string country_code)
        {
            if (string.IsNullOrEmpty(country_code))
            {
                return BadRequest(new { code = "400", msg = "country_code is required" });
            }

            var result = await _locationService.GetStatesAsync(country_code);

            if (result.Code != "200")
            {
                if (int.TryParse(result.Code, out int statusCode))
                {
                    return StatusCode(statusCode, result);
                }
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        [HttpGet("citys")]
        public async Task<IActionResult> GetCities([FromQuery] string country_code, [FromQuery] string state_code)
        {
            if (string.IsNullOrEmpty(country_code) || string.IsNullOrEmpty(state_code))
            {
                return BadRequest(new { code = "400", msg = "country_code and state_code are required" });
            }

            var result = await _locationService.GetCitiesAsync(country_code, state_code);

            if (result.Code != "200")
            {
                if (int.TryParse(result.Code, out int statusCode))
                {
                    return StatusCode(statusCode, result);
                }
                return StatusCode(500, result);
            }

            return Ok(result);
        }

        [HttpGet("asn")]
        public async Task<IActionResult> GetAsns([FromQuery] string country_code)
        {
            if (string.IsNullOrEmpty(country_code))
            {
                return BadRequest(new { code = "400", msg = "country_code is required" });
            }

            var result = await _locationService.GetAsnsAsync(country_code);

            if (result.Code != "200")
            {
                if (int.TryParse(result.Code, out int statusCode))
                {
                    return StatusCode(statusCode, result);
                }
                return StatusCode(500, result);
            }

            return Ok(result);
        }
    }
}
