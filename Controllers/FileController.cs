using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Bewegdeal.Controllers
{
    /// <summary>
    /// Serves files stored by LocalFileStorageService.
    /// Not used when the R2 provider is active — R2 files are served directly via their public URL.
    /// </summary>
    public class FileController(IConfiguration configuration) : Controller
    {
        private readonly string _basePath = configuration["Storage:Local:Path"] ?? "";

        [HttpGet]
        public IActionResult Download(string key)
        {
            // Reject keys containing path separators or traversal sequences
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
