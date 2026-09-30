using GuvenleAlSat.Business.DTOs;
using GuvenleAlSat.Core.Utilities.Results;
using GuvenleAlSat.DataAccess.Entities.Listings;

namespace GuvenleAlSat.Business.Abstract;

public interface IListingService
{
    // İlan Oluşturma ve Detay
    Task<IDataResult<long>> CreateListingAsync(CreateListingDto dto, Guid userId);
    Task<IDataResult<Listing>> GetByListingNoAsync(long listingNo);
    Task<IDataResult<List<Listing>>> GetActiveListingsAsync(int page = 1, int pageSize = 20);

    // Cloudflare R2 / S3 Presigned URL Üretici
    Task<IDataResult<UploadImageResponseDto>> GenerateImageUploadUrlAsync(UploadImageRequestDto dto, Guid userId);

    // Parametrik Arama ve Sol Menü Filtreleme (Faceted Search)
    Task<IDataResult<PagedListingResultDto>> SearchListingsAsync(ListingFilterRequestDto filter);

    // "Ara" Butonuna Basıldığında Satıcı Telefon Numarasını Getirme
    Task<IDataResult<string>> GetSellerPhoneNumberAsync(long listingNo);
    Task<IDataResult<List<ListingCardDto>>> GetUserListingsAsync(Guid userId);
}