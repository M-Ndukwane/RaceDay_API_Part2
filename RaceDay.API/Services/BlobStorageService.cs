using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString = configuration["AzureBlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");

            string containerName = "raceday-images";

            BlobServiceClient blobServiceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            string uniqueFileName =
                $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(uniqueFileName);

            await blobClient.UploadAsync(
                fileStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

            return uniqueFileName;
        }

        public async Task<bool> DeleteImageAsync(string fileName)
        {
            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            var response = await blobClient.DeleteIfExistsAsync();

            return response.Value;
        }
        public async Task<(Stream Content, string ContentType)> DownloadImageAsync(
    string fileName)
        {
            BlobClient blobClient =
                _containerClient.GetBlobClient(fileName);

            var response = await blobClient.DownloadStreamingAsync();

            string contentType =
                response.Value.Details.ContentType ?? "application/octet-stream";

            return (response.Value.Content, contentType);
        }
    }
}
