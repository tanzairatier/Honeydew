namespace Honeydew.Controllers; 

using Microsoft.AspNetCore.Mvc;

[ApiController, Route("api/[controller]")]
public class HealthController() : ControllerBase
{
    [HttpGet("status")]
    public Task<string> Get()
    {
        return Task.FromResult("Healthy");
    }
}
