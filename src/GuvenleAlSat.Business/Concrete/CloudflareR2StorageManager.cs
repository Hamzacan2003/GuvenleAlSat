using Amazon.S3;
using Amazon.S3.Model;
using GuvenleAlSat.Business.Abstract;
using Microsoft.Extensions.Configuration;

namespace GuvenleAlSat.Business.Concrete;

public class CloudflareR2StorageManager : IStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _publicDomain;

    public CloudflareR2StorageManager(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["CloudflareR2:BucketName"] ?? "guvenlealsat-media";
        _publicDomain = configuration["CloudflareR2:PublicDomain"] ?? "https://media.guvenlealsat.com";
    }

    public string GetPresignedUploadUrl(string fileName, string contentType, out string storageKey)
    {
        var extension = Path.GetExtension(fileName);
        storageKey = $"listings/{Guid.NewGuid()}{extension}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(10),
            ContentType = contentType
        };

        return _s3Client.GetPreSignedURL(request);
    }

    public string GetFileUrl(string storageKey)
    {
        return $"{_publicDomain.TrimEnd('/')}/{storageKey}";
    }
}