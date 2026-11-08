using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IImageStorage
{
    /// <summary>Uploads an image to Azure Blob Storage and returns its public URL.</summary>
    Task<string> UploadAsync(string container, string blobName, Stream content, string contentType);
}

public class BlobImageStorage : IImageStorage
{
    private readonly BlobServiceClient _client;

    public BlobImageStorage(BlobServiceClient client)
    {
        _client = client;
    }

    public async Task<string> UploadAsync(string container, string blobName, Stream content, string contentType)
    {
        var containerClient = _client.GetBlobContainerClient(container);

        // Images are public-read so the browser can show them straight from Blob Storage
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

        var blob = containerClient.GetBlobClient(blobName);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        return blob.Uri.ToString();
    }
}

// Used when no connection string is configured, so the API still starts
public class MissingImageStorage : IImageStorage
{
    public Task<string> UploadAsync(string container, string blobName, Stream content, string contentType)
    {
        throw new InvalidOperationException(
            "Azure Blob Storage is not configured. Set AzureBlob:ConnectionString.");
    }
}

public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };

    // Returns an error message, or null when the file is acceptable
    public static string? Check(IFormFile? file, out string extension)
    {
        extension = string.Empty;

        if (file == null || file.Length == 0)
            return "Choose an image file to upload.";

        if (file.Length > MaxBytes)
            return "The image must be 5 MB or smaller.";

        if (!Allowed.TryGetValue(file.ContentType, out var ext))
            return "Only JPG, PNG or WEBP images are allowed.";

        extension = ext;
        return null;
    }
}
