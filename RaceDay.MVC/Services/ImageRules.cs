namespace RaceDay.MVC.Services
{
    // Friendly client-side check so users see a clear message before the upload is attempted.
    public static class ImageRules
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
        public static string? Validate(Microsoft.AspNetCore.Http.IFormFile file)
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
