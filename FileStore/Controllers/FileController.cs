using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.WebEncoders.Testing;

namespace FileStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private const string FileName = "/tmp/text/test.txt";

        [HttpGet]
        public string Get()
        {
            if (!System.IO.File.Exists(FileName)) return "";
            using var sr = System.IO.File.OpenText(FileName);
            return sr.ReadToEnd();
        }

        [HttpPost]
        public IActionResult Post(string input)
        {
            using var streamWriter = System.IO.File.CreateText(FileName);
            streamWriter.WriteLine(input);
            return Ok();
        }
    }
}
