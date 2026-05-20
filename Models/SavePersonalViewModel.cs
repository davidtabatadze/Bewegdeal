namespace Bewegdeal.Models
{
    public class SavePersonalViewModel
    {
        public string? Name { get; set; } = string.Empty;
        public string? Number { get; set; } = string.Empty;
        public string? Mobile { get; set; } = string.Empty;
        public string? Address { get; set; } = string.Empty;
        public string[]? Interests { get; set; }
        public IFormFile? ServiceTermsFile { get; set; }
        public bool DeleteServiceTerms { get; set; }
    }
}
