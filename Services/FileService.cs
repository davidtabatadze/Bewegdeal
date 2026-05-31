using Bewegdeal.Data.Base;
using Bewegdeal.Data.Entities;
using Bewegdeal.Data.Repositories.Abstractions;
using Bewegdeal.Models;
using Bewegdeal.Tools;

namespace Bewegdeal.Services
{
    public class FileService(IFileRepository FileRepository, IFileStorageTool StorageTool)
    {

        public async Task<List<FileEntity>> Load(BaseFilter filter)
            => await FileRepository.Load(filter);

        public async Task<FileEntity?> Get(long? id, string[]? properties = null)
            => id is not null ? await FileRepository.Get<FileEntity>(id.Value, properties) : null;

        public async Task<string?> GetUrl(long? id, string? baseUrl = null)
            => GetUrl(await Get(id, [nameof(FileEntity.Key)]), baseUrl);

        public async Task<ResultModel> Create(IFormFile file, long? replaceId, short? maxSize, string[] allowedTypes)
        {
            // validate type
            if (allowedTypes.Length > 0 && !allowedTypes.Contains(file.ContentType))
            {
                return ResultModel.Fail(
                     $"Invalid file type uploaded. Accepted type(s): {string.Join(", ", allowedTypes.Select(m => m.Split('/').Last().ToUpper()))}."
                );
            }

            // validate size
            if (maxSize.HasValue && (maxSize.Value * 1024 * 1024) < file.Length)
            {
                return ResultModel.Fail(
                     $"Invalid file size uploaded. Accepted size: {maxSize.Value} MB."
                );
            }

            // upload
            using var stream = file.OpenReadStream();
            var key = await StorageTool.Create(stream, file.FileName, file.ContentType);

            // create new
            var entity = await FileRepository.Create(new FileEntity
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
            return ResultModel.Ok(entity.Id);
        }

        public async Task Delete(long id)
        {
            var record = await Get(id, [nameof(FileEntity.Key)]);
            if (record is not null)
            {
                await FileRepository.Delete<FileEntity>(id);
                await StorageTool.Delete(record.Key);
            }
        }

        public string? GetUrl(FileEntity? file, string? baseUrl = null)
        {
            if (file is null) { return null; }
            var relative = StorageTool.GetUrl(file.Key);
            return string.IsNullOrWhiteSpace(baseUrl) ? relative : $"{baseUrl}{relative}";
        }

    }
}