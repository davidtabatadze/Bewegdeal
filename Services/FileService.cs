using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class FileService(IFileStorageTool storageTool, IFileRepository fileRepository)
    {
        public async Task<(long? Id, string? Error)> Create(IFormFile file, long? replaceId, params string[] allowedMimeTypes)
        {
            // validate
            if (allowedMimeTypes.Length > 0 && !allowedMimeTypes.Contains(file.ContentType))
            {
                return (
                    null,
                    $"Invalid file type(s) uploaded. Accepted type(s): {string.Join(", ", allowedMimeTypes.Select(m => m.Split('/').Last().ToUpper()))}."
                );
            }

            // upload
            using var stream = file.OpenReadStream();
            var key = await storageTool.Create(stream, file.FileName, file.ContentType);

            // create new
            var entity = await fileRepository.Create(new FileEntity
            {
                Key = key,
                FileName = file.FileName,
                MimeType = file.ContentType,
                Size = file.Length
            });

            // delete old
            if (replaceId.HasValue)
            {
                await fileRepository.Delete(replaceId.Value);
            }

            // all good
            return (entity.Id, null);
        }
    }
}
