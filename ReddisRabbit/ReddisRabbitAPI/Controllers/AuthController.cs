using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ReddisRabbitAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        

        // POST api/<AuthController>
        [HttpPost]
        public object Post()
        {
            return new { token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"), id = Guid.NewGuid() };
        }

    }
}
