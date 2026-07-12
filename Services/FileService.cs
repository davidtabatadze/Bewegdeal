using Bewegdeal.Models;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class FileService(IFileStorageTool StorageTool)
    {
        public async Task<GenericResultModel<string>> Create(IFormFile file, string? replaceToken, short? maxSize, string[] allowedTypes)
        {
            var fileName = file.FileName;
            var fileLength = file.Length;
            var fileContentType = file.ContentType;

            // validate type
            if (allowedTypes.Length > 0 && !allowedTypes.Contains(fileContentType))
            {
                return GenericResultModel<string>.Fail(
                     // $"Invalid file type uploaded. Accepted type(s): {string.Join(", ", allowedTypes.Select(m => m.Split('/').Last().ToUpper()))}."
                     $"Ungültiger Dateityp. Akzeptierte Typen: {string.Join(", ", allowedTypes.Select(m => m.Split('/').Last().ToUpper()))}."
                );
            }

            // validate size
            if (maxSize.HasValue && (maxSize.Value * 1024 * 1024) < fileLength)
            {
                return GenericResultModel<string>.Fail(
                     // $"Invalid file size uploaded. Accepted size: {maxSize.Value} MB."
                     $"Ungültige Dateigröße. Maximale Größe: {maxSize.Value} MB."
                );
            }

            // validate name
            if (fileName is null || fileName.Length > 128)
            {
                return GenericResultModel<string>.Fail(
                     // $"Invalid file name uploaded. Accepted name length: 128 characters."
                     $"Ungültiger Dateiname. Maximale Länge: 128 Zeichen."
                );
            }

            // upload
            using var stream = file.OpenReadStream();
            var key = await StorageTool.Create(stream, fileName, fileContentType);

            // token
            var token = ComposeToken(key, fileName);

            // delete old
            await Delete(replaceToken);

            // all good
            return GenericResultModel<string>.Ok(token);
        }

        public async Task Delete(string? token)
        {
            if (token is not null)
            {
                await StorageTool.Delete(DecomposeToken(token));
            }
        }

        //public List<string> GetUrls(List<string?> tokens, string? baseUrl = null)
        //    => [.. (tokens ?? []).Select(i => GetUrl(i, baseUrl)).Where(i => i is not null)];

        public string? GetUrl(string? token, string? baseUrl = null)
            => token is not null ? $"{baseUrl ?? ""}{StorageTool.GetUrl(DecomposeToken(token))}" : null;

        public string? GetName(string? token)
            => token is not null ? Uri.UnescapeDataString(token.Replace(DecomposeToken(token) + "=", "")) : null;

        private static string ComposeToken(string key, string name)
            => key + "=" + Uri.EscapeDataString(name);
        private static string DecomposeToken(string? token)
            => token is null ? "-" : Uri.UnescapeDataString(token.Split('=')[0]);
    }
}