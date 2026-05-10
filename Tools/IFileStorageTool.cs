namespace Bewegdeal.Tools
{
    /// <summary>
    /// Abstracts the file storage backend.
    /// The active implementation is registered in Program.cs and
    /// configured via Storage:Local:Path in appsettings.json.
    /// </summary>
    public interface IFileStorageTool
    {
        /// <summary>
        /// Uploads a file and returns the storage key (unique identifier within the backend).
        /// </summary>
        Task<string> Create(Stream stream, string fileName, string mimeType);

        /// <summary>
        /// Deletes the file identified by <paramref name="key"/> from storage.
        /// </summary>
        Task Delete(string key);

        /// <summary>
        /// Returns a URL that can be used to download the file identified by <paramref name="key"/>.
        /// </summary>
        string GetUrl(string key);
    }
}
