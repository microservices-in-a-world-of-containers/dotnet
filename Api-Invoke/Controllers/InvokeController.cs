using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Api_Invoke.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvokeController : ControllerBase
    {
        [HttpGet]
        public async Task<string> Get()
        {
            var httpClient = new HttpClient();
            var httpResponseMessage = await httpClient.GetStringAsync("http://api-service:80/api/environment");
            return httpResponseMessage;
        }
    }
}
