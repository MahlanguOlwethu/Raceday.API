using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    // Thrown when an upload is attempted but Azure Blob Storage has not been configured.
    public class BlobStorageNotConfiguredException : InvalidOperationException
    {
        public BlobStorageNotConfiguredException()
            : base("Azure Blob Storage is not configured. Set AzureBlob:ConnectionString.") { }
    }

    public class BlobService
    {
        private readonly string? _connectionString;

        public BlobService(IConfiguration configuration)
        {
            // Reads the Docker variable (AzureBlob__ConnectionString) or the local
            // ConnectionStrings:AzureBlobStorage value. A missing value is NOT an error here:
            // only uploads need Azure, so every other endpoint keeps working without it.
            var value = configuration["AzureBlob:ConnectionString"];
            if (string.IsNullOrWhiteSpace(value))
                value = configuration.GetConnectionString("AzureBlobStorage");

            _connectionString = string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public bool IsConfigured => _connectionString != null;

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            if (_connectionString == null) throw new BlobStorageNotConfiguredException();

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Create the container if it doesn't exist, with anonymous read access to blobs so
            // the MVC app can display the image straight from its URL.
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString() + Path.GetExtension(file.FileName).ToLowerInvariant());

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blobClient.Uri.ToString();
        }
    }
}
