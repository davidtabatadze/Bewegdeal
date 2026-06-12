using Bewegdeal.Data.Entities;

namespace Bewegdeal.Models
{
    public class UserProfileModel
    {
        public UserEntity User { get; set; } = new UserEntity();
        public string? ServiceTermsFileUrl { get; set; } = null;
        public string? ServiceTermsFileName { get; set; } = null;
        public UserAvatarModel Avatar { get; set; } = new UserAvatarModel();
    }
}
