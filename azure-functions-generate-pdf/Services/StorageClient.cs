using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace GeneratePdf.Services
{
    public class StorageClient : IStorageClient
    {
        private readonly IConfiguration _config;
        private readonly BlobContainerClient _templateContainerClient;
        private readonly BlobContainerClient _outputContainerClient;

        public StorageClient(IConfiguration config)
        {
            _config = config;
            _templateContainerClient = new BlobContainerClient(config["AzureWebJobsStorage"], config["TemplateContainer"]);
            if (!_templateContainerClient.Exists()) {
                throw new DirectoryNotFoundException($"Blob container '{config["TemplateContainer"]}' needs to exist and contain template files.");
            }
            _outputContainerClient = new BlobContainerClient(config["AzureWebJobsStorage"], config["OutputContainer"]);
            _outputContainerClient.CreateIfNotExists();
        }

        public async Task DownloadTemplateAsync(string blobName)
        {
            var blob = _templateContainerClient.GetBlobClient(blobName);
            await blob.DownloadToAsync(Path.Combine(_config["TempFolderPath"], blobName));
        }

        public async Task UploadFileResultAsync(string blobName, Stream content)
        {
            var blob = _outputContainerClient.GetBlobClient(blobName);
            content.Position = 0;
            await blob.UploadAsync(content);
        }
    }

    public interface IStorageClient
    {
        Task DownloadTemplateAsync(string blobName);
        Task UploadFileResultAsync(string blobName, Stream content);
    }
}
