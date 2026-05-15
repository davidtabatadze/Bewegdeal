using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class FileService(IFileStorageTool storageTool, IFileRepository fileRepository)
    {

        public async Task<(long? Id, string? Error)> Create(IFormFile file, long? replaceId, short? maxSize, string[] allowedTypes)
        {
            // validate type
            if (allowedTypes.Length > 0 && !allowedTypes.Contains(file.ContentType))
            {
                return (
                    null,
                    $"Invalid file type(s) uploaded. Accepted type(s): {string.Join(", ", allowedTypes.Select(m => m.Split('/').Last().ToUpper()))}."
                );
            }

            // validate size
            if (maxSize.HasValue && (maxSize.Value * 1024 * 1024) < file.Length)
            {
                return (
                    null,
                    $"Invalid file size uploaded. Accepted size: {maxSize.Value} MB."
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
                await Delete(replaceId.Value);
            }

            // all good
            return (entity.Id, null);
        }

        public async Task Delete(long id)
        {
            var record = await fileRepository.Get(id);
            if (record is not null)
            {
                await fileRepository.Delete(record.Id);
                await storageTool.Delete(record.Key);
            }
        }

    }
}
