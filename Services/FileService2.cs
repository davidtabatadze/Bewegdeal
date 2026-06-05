using Bewegdeal.Models;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class FileService2(IFileStorageTool StorageTool)
    {
        public async Task<GenericResultModel<string>> Create(IFormFile file, string? replaceFile, short? maxSize, string[] allowedTypes)
        {
            var fileName = file.FileName;
            var fileLength = file.Length;
            var fileContentType = file.ContentType;

            // validate type
            if (allowedTypes.Length > 0 && !allowedTypes.Contains(fileContentType))
            {
                return GenericResultModel<string>.Fail(
                     $"Invalid file type uploaded. Accepted type(s): {string.Join(", ", allowedTypes.Select(m => m.Split('/').Last().ToUpper()))}."
                );
            }

            // validate size
            if (maxSize.HasValue && (maxSize.Value * 1024 * 1024) < fileLength)
            {
                return GenericResultModel<string>.Fail(
                     $"Invalid file size uploaded. Accepted size: {maxSize.Value} MB."
                );
            }

            // validate name
            if (fileName is null || fileName.Length > 128)
            {
                return GenericResultModel<string>.Fail(
                     $"Invalid file name uploaded. Accepted name length: 128 characters."
                );
            }

            // upload
            using var stream = file.OpenReadStream();
            var key = await StorageTool.Create(stream, fileName, fileContentType);

            // kvp
            var kvp = ComposeKVP(key, fileName);

            // delete old
            await Delete(replaceFile);

            // all good
            return GenericResultModel<string>.Ok(kvp);
        }

        public async Task Delete(string? kvp)
        {
            if (kvp is not null)
            {
                await StorageTool.Delete(DecomposeKVP(kvp));
            }
        }

        //public List<string> GetUrls(List<string?> kvps, string? baseUrl = null)
        //    => [.. (kvps ?? []).Select(i => GetUrl(i, baseUrl)).Where(i => i is not null)];

        public string? GetUrl(string? kvp, string? baseUrl = null)
            => kvp is not null ? $"{baseUrl ?? ""}{StorageTool.GetUrl(DecomposeKVP(kvp))}" : null;

        public string? GetName(string? kvp)
            => kvp is not null ? Uri.UnescapeDataString(kvp.Replace(DecomposeKVP(kvp) + "=", "")) : null;

        private static string ComposeKVP(string key, string name)
            => key + "=" + Uri.EscapeDataString(name);
        private static string DecomposeKVP(string? kvp)
            => kvp is null ? "-" : Uri.UnescapeDataString(kvp.Split('=')[0]);
    }
}