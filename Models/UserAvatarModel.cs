namespace Bewegdeal.Models
{
    public class UserAvatarModel
    {
        public string Initials { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Url { get; set; }

        public UserAvatarModel()
        {
            Initials = "??";
            Name = "Undefined";
            Url = null;
        }
    }
}
