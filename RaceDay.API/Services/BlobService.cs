using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    // Thrown when an upload cannot be done: Blob Storage is not configured or cannot be reached.
    // The controllers turn it into a friendly 503 response instead of a crash.
    public class BlobStorageNotConfiguredException : InvalidOperationException
    {
        public BlobStorageNotConfiguredException()
            : base("Azure Blob Storage is not configured. Set AzureBlob:ConnectionString.") { }

        public BlobStorageNotConfiguredException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class BlobService
    {
        private readonly string? _connectionString;
        private readonly string? _publicBaseUrl;

        public BlobService(IConfiguration configuration)
        {
            // Reads the Docker variable (AzureBlob__ConnectionString) or the local
            // ConnectionStrings:AzureBlobStorage value. A missing value is NOT an error here:
            // only uploads need Azure, so every other endpoint keeps working without it.
            var value = configuration["AzureBlob:ConnectionString"];
            if (string.IsNullOrWhiteSpace(value))
                value = configuration.GetConnectionString("AzureBlobStorage");

            _connectionString = string.IsNullOrWhiteSpace(value) ? null : value;

            // Optional. Used with Azurite in Docker: the API uploads to http://azurite:10000,
            // but the browser must load images from http://localhost:10000.
            var publicUrl = configuration["AzureBlob:PublicBaseUrl"];
            _publicBaseUrl = string.IsNullOrWhiteSpace(publicUrl) ? null : publicUrl.TrimEnd('/');
        }

        public bool IsConfigured => _connectionString != null;

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            if (_connectionString == null) throw new BlobStorageNotConfiguredException();

            try
            {
                // Fail fast instead of retrying for a long time if storage is down
                var options = new BlobClientOptions();
                options.Retry.MaxRetries = 2;

                var blobServiceClient = new BlobServiceClient(_connectionString, options);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Create the container if it doesn't exist, with anonymous read access to blobs so
                // the MVC app can display the image straight from its URL.
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString() + Path.GetExtension(file.FileName).ToLowerInvariant());

                using var stream = file.OpenReadStream();
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

                var url = blobClient.Uri.ToString();
                if (_publicBaseUrl != null)
                    url = url.Replace(blobServiceClient.Uri.ToString().TrimEnd('/'), _publicBaseUrl);
                return url;
            }
            catch (Exception ex)
            {
                throw new BlobStorageNotConfiguredException("Azure Blob Storage could not be reached.", ex);
            }
        }
    }
}
