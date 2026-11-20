namespace RaceDay.API.Services
{
    // Server-side guard for every image that is about to be sent to Azure Blob Storage.
    public static class ImageUploadRules
    {
        public const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        private static readonly Dictionary<string, string[]> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = new[] { ".jpg", ".jpeg" },
            ["image/png"]  = new[] { ".png" },
            ["image/gif"]  = new[] { ".gif" },
            ["image/webp"] = new[] { ".webp" }
        };

        // Returns an error message, or null when the file is acceptable.
        public static string? Validate(IFormFile file)
        {
            if (file.Length > MaxBytes)
                return "The image must be 5 MB or smaller.";

            var ext = Path.GetExtension(file.FileName);
            if (!Allowed.TryGetValue(file.ContentType ?? string.Empty, out var exts) ||
                !exts.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return "Only JPG, PNG, GIF or WEBP images are allowed.";

            return null;
        }
    }
}
