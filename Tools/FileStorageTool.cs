namespace Bewegdeal.Tools
{
    /// <summary>
    /// Stores files on the local file system.
    /// Configured via Storage:Local:Path in appsettings.json.
    /// Files are served through FileController — never exposed directly from disk.
    /// </summary>
    public class FileStorageTool : IFileStorageTool
    {
        private readonly string _basePath;

        public FileStorageTool(IConfiguration configuration, IWebHostEnvironment environment)
        {
            var configPath = configuration["Storage:Local:Path"]
                ?? throw new InvalidOperationException("Storage:Local:Path is not configured.");

            // Relative paths are resolved against the application content root,
            // so "Storage" in appsettings maps to the Storage/ folder in the solution.
            _basePath = Path.IsPathRooted(configPath)
                ? configPath
                : Path.Combine(environment.ContentRootPath, configPath);

            Directory.CreateDirectory(_basePath);
        }

        public async Task<string> Upload(Stream stream, string fileName, string mimeType)
        {
            var extension = Path.GetExtension(fileName);
            var key = Guid.NewGuid().ToString("N") + extension.ToLower();
            var fullPath = Path.Combine(_basePath, key);

            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);

            return key;
        }

        public Task Delete(string key)
        {
            var fullPath = Path.Combine(_basePath, key);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        public string GetUrl(string key) => $"/File/Download/{key}";
    }
}
