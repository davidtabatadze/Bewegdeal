using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Bewegdeal.Controllers
{
    public class FileController(IConfiguration configuration, IWebHostEnvironment environment) : XBaseController
    {
        private readonly string _basePath = Path.IsPathRooted(configuration["Storage:Local:Path"] ?? "")
            ? configuration["Storage:Local:Path"]!
            : Path.Combine(environment.ContentRootPath, configuration["Storage:Local:Path"] ?? "");

        [HttpGet]
        public IActionResult Download(string key)
        {
            if (string.IsNullOrWhiteSpace(key) ||
                key.Contains('/') ||
                key.Contains('\\') ||
                key.Contains(".."))
            {
                return BadRequest();
            }

            var fullPath = Path.Combine(_basePath, key);

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(key, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
        }
    }
}
