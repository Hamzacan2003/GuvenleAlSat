using GuvenleAlSat.Core.Utilities.Results;

namespace GuvenleAlSat.Business.Abstract;

public interface IStorageService
{
    string GetPresignedUploadUrl(string fileName, string contentType, out string storageKey);
    string GetFileUrl(string storageKey);
}