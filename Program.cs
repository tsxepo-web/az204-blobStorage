using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

class AzureBlobStorageExample
{
    private const string StorageConnectionString = "YourAzureStorageConnectionString";

    static void Main()
    {
        Console.WriteLine("Azure Blob Storage exercise\n");

        ProcessAsync().GetAwaiter().GetResult();

        Console.WriteLine("Press enter to exit the sample application.");
        Console.ReadLine();
    }

    private static async Task ProcessAsync()
    {
        BlobContainerClient containerClient = await CreateBlobContainerAsync();
        await ReadContainerPropertiesAsync(containerClient);
        await AddContainerMetadataAsync(containerClient);
        await ReadContainerMetadataAsync(containerClient);  // New method call added here
        string localFilePath = await UploadFileToBlobAsync(containerClient);
        await ListBlobsAsync(containerClient);
        await DownloadBlobAsync(containerClient, localFilePath);
        await CleanupAsync(containerClient, localFilePath);
    }

    private static async Task<BlobContainerClient> CreateBlobContainerAsync()
    {
        BlobServiceClient blobServiceClient = new BlobServiceClient(StorageConnectionString);
        string containerName = "wtblob" + Guid.NewGuid();

        BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
        Console.WriteLine($"A container named '{containerName}' has been created.\nVerify it in the portal.");
        PromptToContinue();

        return containerClient;
    }

    private static async Task ReadContainerPropertiesAsync(BlobContainerClient container)
    {
        try
        {
            var properties = await container.GetPropertiesAsync();
            Console.WriteLine($"Properties for container {container.Uri}");
            Console.WriteLine($"Public access level: {properties.Value.PublicAccess}");
            Console.WriteLine($"Last modified time in UTC: {properties.Value.LastModified}");
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }

    private static async Task AddContainerMetadataAsync(BlobContainerClient container)
    {
        try
        {
            IDictionary<string, string> metadata = new Dictionary<string, string>
            {
                { "docType", "textDocuments" },
                { "category", "guidance" }
            };

            await container.SetMetadataAsync(metadata);
            Console.WriteLine($"Metadata added to container {container.Uri}");
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }

    private static async Task ReadContainerMetadataAsync(BlobContainerClient container)
    {
        try
        {
            var properties = await container.GetPropertiesAsync();

            // Enumerate the container's metadata.
            Console.WriteLine("Container metadata:");
            foreach (var metadataItem in properties.Value.Metadata)
            {
                Console.WriteLine($"\tKey: {metadataItem.Key}");
                Console.WriteLine($"\tValue: {metadataItem.Value}");
            }
        }
        catch (RequestFailedException e)
        {
            Console.WriteLine($"HTTP error code {e.Status}: {e.ErrorCode}");
            Console.WriteLine(e.Message);
            Console.ReadLine();
        }
    }

    private static async Task<string> UploadFileToBlobAsync(BlobContainerClient containerClient)
    {
        string localPath = "./data/";
        string fileName = "wtfile" + Guid.NewGuid() + ".txt";
        string localFilePath = Path.Combine(localPath, fileName);

        await File.WriteAllTextAsync(localFilePath, "Hello, World!");

        BlobClient blobClient = containerClient.GetBlobClient(fileName);

        Console.WriteLine($"Uploading to Blob Storage as blob:\n\t {blobClient.Uri}\n");

        using (FileStream uploadFileStream = File.OpenRead(localFilePath))
        {
            await blobClient.UploadAsync(uploadFileStream);
        }

        Console.WriteLine("\nThe file was uploaded. Listing the blobs next.");
        PromptToContinue();

        return localFilePath;
    }

    private static async Task ListBlobsAsync(BlobContainerClient containerClient)
    {
        Console.WriteLine("Listing the blobs...");
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        {
            Console.WriteLine($"\t{blobItem.Name}");
        }

        Console.WriteLine("\nVerify in the portal. Next, the blob will be downloaded with an altered file name.");
        PromptToContinue();
    }

    private static async Task DownloadBlobAsync(BlobContainerClient containerClient, string localFilePath)
    {
        string downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");

        BlobClient blobClient = containerClient.GetBlobClient(Path.GetFileName(localFilePath));

        Console.WriteLine($"\nDownloading blob to\n\t{downloadFilePath}\n");

        BlobDownloadInfo download = await blobClient.DownloadAsync();

        using (FileStream downloadFileStream = File.OpenWrite(downloadFilePath))
        {
            await download.Content.CopyToAsync(downloadFileStream);
        }

        Console.WriteLine("Verify the downloaded file in the data directory.");
        PromptToContinue();
    }

    private static async Task CleanupAsync(BlobContainerClient containerClient, string localFilePath)
    {
        Console.WriteLine("\n\nDeleting blob container...");
        await containerClient.DeleteAsync();

        string downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");

        Console.WriteLine("Deleting the local source and downloaded files...");
        File.Delete(localFilePath);
        File.Delete(downloadFilePath);

        Console.WriteLine("Finished cleaning up.");
    }

    private static void PromptToContinue()
    {
        Console.WriteLine("Press 'Enter' to continue.");
        Console.ReadLine();
    }
}
