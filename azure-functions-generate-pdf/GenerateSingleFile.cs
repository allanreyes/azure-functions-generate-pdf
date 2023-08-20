using GeneratePdf.Services;
using HiQPdf;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GeneratePdf
{
    public class GenerateSingleFile
    {
        private readonly IStorageClient _storageClient;
        private readonly IConfiguration _config;

        public GenerateSingleFile(IConfiguration config, IStorageClient storageClient)
        {
            _config = config;
            _storageClient = storageClient;
        }

        [FunctionName(nameof(GenerateSingleFile))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext executionContext,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Download blob template to temp folder
            await _storageClient.DownloadTemplateAsync(_config["TemplateFilename"]);

            // Read html content
            var html = await File.ReadAllTextAsync(Path.Combine(_config["TempFolderPath"], _config["TemplateFilename"]));

            // Replace tokens
            var output = html.Replace("{{ NAME }}", "Allan");

            // Generate PDF
            var htmlToPdf = new HtmlToPdf();
            var depPath = Path.Combine(executionContext.FunctionAppDirectory, "HiQPdf", "HiQPdf.dep");
            htmlToPdf.SetDepFilePath(depPath);
            
            using (var s = new MemoryStream()) {
                htmlToPdf.ConvertHtmlToStream(output, null, s);

                // Upload PDF to blob storage
                var blobName = $"{Guid.NewGuid()}.pdf";

                await _storageClient.UploadFileResultAsync(blobName, s);
            }

            return new OkResult();
        }
    }
}
