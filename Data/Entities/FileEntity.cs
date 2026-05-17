using Bewegdeal.Data.Base;

namespace Bewegdeal.Data.Entities
{
    public class FileEntity : IEntity
    {
        public long Id { get; set; }
        public string Key { get; set; } = "";
        public string FileName { get; set; } = "";
        public string MimeType { get; set; } = "";
        public long Size { get; set; }
    }
}
