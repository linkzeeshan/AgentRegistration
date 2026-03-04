using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using MkTechSys.AgentRegistrationForm.Models;
using System.Text;

namespace MkTechSys.AgentRegistrationForm.Services
{
    public class AgentRegistrationBlobCsvService : IAgentRegistrationStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly string _blobName;
        private readonly ILogger<AgentRegistrationBlobCsvService> _logger;
        private const string CsvHeader = "FirstName,LastName,Email,CRCode,IqamaOrNationalId,Telephone,SubmittedAtUtc";

        public AgentRegistrationBlobCsvService(
            IConfiguration configuration,
            ILogger<AgentRegistrationBlobCsvService> logger)
        {
            _logger = logger;
            
            var storageConfig = configuration.GetSection("AgentRegistrationStorage");
            _containerName = storageConfig["ContainerName"] ?? "registrations";
            _blobName = storageConfig["BlobName"] ?? "main.csv";

            var connectionString = storageConfig["ConnectionString"];
            var accountName = storageConfig["AccountName"];

            if (!string.IsNullOrEmpty(connectionString))
            {
                _blobServiceClient = new BlobServiceClient(connectionString);
                _logger.LogInformation("Initialized Blob Storage using connection string");
            }
            else if (!string.IsNullOrEmpty(accountName))
            {
                var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
                _blobServiceClient = new BlobServiceClient(blobUri, new DefaultAzureCredential());
                _logger.LogInformation("Initialized Blob Storage using Managed Identity for account: {AccountName}", accountName);
            }
            else
            {
                throw new InvalidOperationException("Azure Storage configuration is missing. Please provide either ConnectionString or AccountName in AgentRegistrationStorage section.");
            }
        }

        public async Task<bool> SaveRegistrationAsync(AgentRegistrationViewModel model)
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString();
                _logger.LogInformation("Starting registration save. CorrelationId: {CorrelationId}", correlationId);

                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                var blobClient = containerClient.GetBlobClient(_blobName);
                var leaseClient = blobClient.GetBlobLeaseClient();

                string? leaseId = null;
                try
                {
                    var csvRow = GenerateCsvRow(model);

                    bool blobExists = await blobClient.ExistsAsync();

                    if (!blobExists)
                    {
                        var initialContent = $"{CsvHeader}\n{csvRow}\n";
                        var bytes = Encoding.UTF8.GetBytes(initialContent);
                        using var stream = new MemoryStream(bytes);
                        await blobClient.UploadAsync(stream, overwrite: false);
                        _logger.LogInformation("Created new CSV blob with header. CorrelationId: {CorrelationId}", correlationId);
                    }
                    else
                    {
                        var leaseResponse = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(60));
                        leaseId = leaseResponse.Value.LeaseId;
                        _logger.LogDebug("Acquired blob lease. LeaseId: {LeaseId}, CorrelationId: {CorrelationId}", leaseId, correlationId);

                        var downloadResponse = await blobClient.DownloadContentAsync(new BlobDownloadOptions
                        {
                            Conditions = new BlobRequestConditions { LeaseId = leaseId }
                        });

                        var existingContent = downloadResponse.Value.Content.ToString();
                        var updatedContent = existingContent.TrimEnd('\n', '\r') + "\n" + csvRow + "\n";

                        var bytes = Encoding.UTF8.GetBytes(updatedContent);
                        using var stream = new MemoryStream(bytes);
                        
                        await blobClient.UploadAsync(stream, new BlobUploadOptions
                        {
                            Conditions = new BlobRequestConditions { LeaseId = leaseId }
                        });

                        _logger.LogInformation("Appended row to existing CSV. CorrelationId: {CorrelationId}", correlationId);
                    }

                    return true;
                }
                finally
                {
                    if (leaseId != null)
                    {
                        try
                        {
                            await leaseClient.ReleaseAsync();
                            _logger.LogDebug("Released blob lease. LeaseId: {LeaseId}", leaseId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to release blob lease. LeaseId: {LeaseId}", leaseId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save registration to blob storage");
                return false;
            }
        }

        private string GenerateCsvRow(AgentRegistrationViewModel model)
        {
            var submittedAt = DateTime.UtcNow.ToString("o");
            
            var fields = new[]
            {
                EscapeCsvField(model.FirstName),
                EscapeCsvField(model.LastName),
                EscapeCsvField(model.Email),
                EscapeCsvField(model.CRCode),
                EscapeCsvField(model.IqamaOrNationalId),
                EscapeCsvField(model.Telephone),
                EscapeCsvField(submittedAt)
            };

            return string.Join(",", fields);
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                var escaped = field.Replace("\"", "\"\"");
                return $"\"{escaped}\"";
            }

            return $"\"{field}\"";
        }
    }
}
