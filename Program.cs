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
        string containerName = await CreateBlobContainerAsync();
        string localFilePath = await UploadFileToBlobAsync(containerName);
        await ListBlobsAsync(containerName);
        await DownloadBlobAsync(containerName, localFilePath);
        await CleanupAsync(containerName, localFilePath);
    }

    private static async Task<string> CreateBlobContainerAsync()
    {
        BlobServiceClient blobServiceClient = new BlobServiceClient(StorageConnectionString);
        string containerName = "wtblob" + Guid.NewGuid();

        BlobContainerClient containerClient = await blobServiceClient.CreateBlobContainerAsync(containerName);
        Console.WriteLine($"A container named '{containerName}' has been created.\nVerify it in the portal.");
        PromptToContinue();

        return containerName;
    }

    private static async Task<string> UploadFileToBlobAsync(string containerName)
    {
        string localPath = "./data/";
        string fileName = "wtfile" + Guid.NewGuid() + ".txt";
        string localFilePath = Path.Combine(localPath, fileName);

        await File.WriteAllTextAsync(localFilePath, "Hello, World!");

        BlobContainerClient containerClient = new BlobServiceClient(StorageConnectionString).GetBlobContainerClient(containerName);
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

    private static async Task ListBlobsAsync(string containerName)
    {
        BlobContainerClient containerClient = new BlobServiceClient(StorageConnectionString).GetBlobContainerClient(containerName);

        Console.WriteLine("Listing the blobs...");
        await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
        {
            Console.WriteLine($"\t{blobItem.Name}");
        }

        Console.WriteLine("\nVerify in the portal. Next, the blob will be downloaded with an altered file name.");
        PromptToContinue();
    }

    private static async Task DownloadBlobAsync(string containerName, string localFilePath)
    {
        string downloadFilePath = localFilePath.Replace(".txt", "DOWNLOADED.txt");

        BlobContainerClient containerClient = new BlobServiceClient(StorageConnectionString).GetBlobContainerClient(containerName);
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

    private static async Task CleanupAsync(string containerName, string localFilePath)
    {
        BlobContainerClient containerClient = new BlobServiceClient(StorageConnectionString).GetBlobContainerClient(containerName);

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
