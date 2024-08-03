using Microsoft.AspNetCore.Mvc;
using ServiceB.Application.Features.ConfigurationValues.Queries.GetByName;

namespace ServiceB.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigController : BaseController
    {

        [HttpGet("get-setting/{name}")]
        public async Task<IActionResult> GetList(string name)
        {
            GetByNameConfigurationValueQuery getByNameConfigurationValueQuery = new() { Name = name };
            GetByNameConfigurationResponse response = await Mediator.Send(getByNameConfigurationValueQuery);
            return Ok(response);
        }
    }
}
