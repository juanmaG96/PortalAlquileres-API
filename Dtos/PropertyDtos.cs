using Marketplace.API.Data.Entities;

namespace Marketplace.API.Dtos;

public record PropertySummaryDto(
    Guid Id,
    string Title,
    decimal Price,
    string Currency,
    string? CoverImageUrl,
    int Rooms,
    PropertyType PropertyType,
    OfferType OfferType,
    PropertyStatus Status,
    bool IsPremium,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    DateTime CreatedAt,
    string ContactPhone
);

public record PropertyDetailDto(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int Rooms,
    PropertyType PropertyType,
    OfferType OfferType,
    PropertyStatus Status,
    bool IsPremium,
    DateTime CreatedAt,
    string ContactPhone,
    string City,
    string Address,
    double? Latitude,
    double? Longitude,
    List<string> ImageUrls
);

public record PropertyCreateDto(
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int Rooms,
    PropertyType PropertyType,
    OfferType OfferType,
    string ContactPhone,
    string City,
    string Address,
    List<string> ImageUrls,
    bool IsPremium
);

public class PropertySearchFilterDto
{
    public string? City { get; set; }
    public PropertyType? PropertyType { get; set; }
    public OfferType? OfferType { get; set; }
    public string? Keyword { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Rooms { get; set; }
    public bool? OnlyPremium { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public bool IncludeInactive { get; set; } = false;
    public bool ShowDeleted { get; set; } = false;

    public string BuildCacheKey() =>
        $"search_{City}_{PropertyType}_{OfferType}_{Keyword}_{MinPrice}_{MaxPrice}_{Rooms}_{OnlyPremium}_{Page}_{PageSize}_{IncludeInactive}".ToLowerInvariant();
}

public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResultDto(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}

public record GeocodeRequestDto(
    string Address,
    string? City
);

public record GeocodeResultDto(
    string FormattedAddress,
    double Latitude,
    double Longitude,
    bool Success,
    string? ErrorMessage
);
