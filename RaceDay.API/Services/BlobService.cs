using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class BlobService
    {
        private readonly string _connectionString;

        public BlobService(IConfiguration configuration)
        {
            // FIXED: Reads Docker variable (AzureBlob:ConnectionString) OR local appsettings (AzureBlobStorage)
            _connectionString = configuration["AzureBlob:ConnectionString"]
                                ?? configuration.GetConnectionString("AzureBlobStorage")
                                ?? throw new InvalidOperationException("Azure Blob connection string is missing.");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string containerName)
        {
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Create the container if it doesn't exist, and set public access for reading images
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(Guid.NewGuid().ToString() + Path.GetExtension(file.FileName));

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blobClient.Uri.ToString();
        }
    }
}