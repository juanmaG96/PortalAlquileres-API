using Marketplace.API.Data;
using Marketplace.API.Data.Entities;
using Marketplace.API.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Marketplace.API.Services;

public class PropertyService : IPropertyService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly INominatimGeocodingService _geocodingService;
    private readonly ILogger<PropertyService> _logger;
    private readonly string _defaultCity;
    private readonly decimal _exchangeRate;
    private readonly string _defaultCurrency;

    public PropertyService(
        ApplicationDbContext dbContext,
        IMemoryCache cache,
        INominatimGeocodingService geocodingService,
        IConfiguration configuration,
        ILogger<PropertyService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _geocodingService = geocodingService;
        _logger = logger;
        _defaultCity = configuration["WhiteLabelSettings:InstanceCity"] ?? "Paysandú";
        _exchangeRate = decimal.TryParse(configuration["WhiteLabelSettings:ExchangeRate"], out var rate) ? rate : 1m;
        _defaultCurrency = configuration["WhiteLabelSettings:DefaultCurrency"] ?? "UYU";
    }

    public async Task<PagedResultDto<PropertySummaryDto>> GetPropertiesAsync(PropertySearchFilterDto filter, CancellationToken cancellationToken = default)
    {
        string cacheKey = filter.BuildCacheKey();

        // Solo intentamos leer del caché si es una búsqueda pública (no admin)
        if (!filter.IncludeInactive && _cache.TryGetValue(cacheKey, out PagedResultDto<PropertySummaryDto>? cachedResult) && cachedResult != null)
        {
            _logger.LogInformation("Property search cache hit for key: {CacheKey}", cacheKey);
            return cachedResult;
        }

        IQueryable<Property> query = _dbContext.Properties.AsNoTracking();

        query = query.Where(p => !p.IsDeleted);

        // Si NO es el panel de admin (IncludeInactive es false), solo mostramos los Activos
        if (!filter.IncludeInactive)
        {
            query = query.Where(p => p.Status == PropertyStatus.Active);
        }

        // White-label default city filter if not explicitly overridden
        string searchCity = string.IsNullOrWhiteSpace(filter.City) ? _defaultCity : filter.City;
        query = query.Where(p => p.City.ToLower() == searchCity.ToLower());

        if (filter.PropertyType.HasValue)
        {
            query = query.Where(p => p.PropertyType == filter.PropertyType.Value);
        }

        if (filter.OfferType.HasValue)
        {
            query = query.Where(p => p.OfferType == filter.OfferType.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            string kw = filter.Keyword.ToLower().Trim();
            query = query.Where(p => p.Title.ToLower().Contains(kw) || p.Description.ToLower().Contains(kw) || p.Address.ToLower().Contains(kw));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => 
                // Si la propiedad está en la moneda local, compara directo
                (p.Currency == _defaultCurrency && p.Price <= filter.MaxPrice.Value) ||
                
                // Si la propiedad está en USD, multiplica por la tasa de la ciudad antes de comparar
                (p.Currency == "USD" && (p.Price * _exchangeRate) <= filter.MaxPrice.Value)
            );
        }

        if (filter.Rooms.HasValue)
        {
            query = query.Where(p => p.Rooms >= filter.Rooms.Value);
        }

        if (filter.OnlyPremium.HasValue && filter.OnlyPremium.Value)
        {
            query = query.Where(p => p.IsPremium);
        }

        // Total count before pagination
        int totalCount = await query.CountAsync(cancellationToken);

        // Native Pagination (Skip / Take) + Ordering (Premium first, then newest)
        int page = filter.Page < 1 ? 1 : filter.Page;
        int pageSize = filter.PageSize < 1 ? 12 : (filter.PageSize > 50 ? 50 : filter.PageSize);

        var items = await query
            .OrderByDescending(p => p.IsPremium)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            // Explicit projection to PropertySummaryDto avoiding overfetching!
            .Select(p => new PropertySummaryDto(
                p.Id,
                p.Title,
                p.Price,
                p.Currency,
                p.ImageUrls.FirstOrDefault(),
                p.Rooms,
                p.PropertyType,
                p.OfferType,
                p.Status,
                p.IsPremium,
                p.City,
                p.Address,
                p.Latitude,
                p.Longitude,
                p.CreatedAt,
                p.ContactPhone
            ))
            .ToListAsync(cancellationToken);

        var result = new PagedResultDto<PropertySummaryDto>(items, totalCount, page, pageSize);

        // Solo guardamos en caché las peticiones públicas.
        // El panel de admin NUNCA se guarda en caché, siempre trae datos frescos.
        if (!filter.IncludeInactive)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(cacheKey, result, cacheEntryOptions);
        }

        return result;
    }

    public async Task<PagedResultDto<PropertySummaryDto>> GetAdminPropertiesAsync(PropertySearchFilterDto filter, CancellationToken cancellationToken = default)
    {
        IQueryable<Property> query = _dbContext.Properties.AsNoTracking().IgnoreQueryFilters(); // <-- Ignoramos filtros globales (Soft Delete)

        // Solo ocultamos los eliminados (Soft Delete). ¡Traemos Activos e Inactivos!
        //query = query.Where(p => !p.IsDeleted);

        if (filter.ShowDeleted)
        {
            query = query.Where(p => p.IsDeleted == true);
        }
        else
        {
            query = query.Where(p => p.IsDeleted == false);
        }

        // Filtros de búsqueda (igual que en el público)
        string searchCity = string.IsNullOrWhiteSpace(filter.City) ? _defaultCity : filter.City;
        query = query.Where(p => p.City.ToLower() == searchCity.ToLower());

        if (filter.PropertyType.HasValue) query = query.Where(p => p.PropertyType == filter.PropertyType.Value);
        if (filter.OfferType.HasValue) query = query.Where(p => p.OfferType == filter.OfferType.Value);
        
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            string kw = filter.Keyword.ToLower().Trim();
            query = query.Where(p => p.Title.ToLower().Contains(kw) || p.Description.ToLower().Contains(kw) || p.Address.ToLower().Contains(kw));
        }

        // Total count
        int totalCount = await query.CountAsync(cancellationToken);

        // Paginación y Orden (Primero premium, luego por fecha)
        int page = filter.Page < 1 ? 1 : filter.Page;
        int pageSize = filter.PageSize < 1 ? 12 : (filter.PageSize > 50 ? 50 : filter.PageSize);

        var items = await query
            .OrderByDescending(p => p.IsPremium)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PropertySummaryDto(
                p.Id, p.Title, p.Price, p.Currency, p.ImageUrls.FirstOrDefault(),
                p.Rooms, p.PropertyType, p.OfferType, p.Status, p.IsPremium,
                p.City, p.Address, p.Latitude, p.Longitude, p.CreatedAt, p.ContactPhone
            ))
            .ToListAsync(cancellationToken);

        // Retornamos directo a PostgreSQL, CERO caché.
        return new PagedResultDto<PropertySummaryDto>(items, totalCount, page, pageSize);
    }

    public async Task<PropertyDetailDto?> GetPropertyByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (property == null) return null;

        return new PropertyDetailDto(
            property.Id,
            property.Title,
            property.Description,
            property.Price,
            property.Currency,
            property.Rooms,
            property.PropertyType,
            property.OfferType,
            property.Status,
            property.IsPremium,
            property.CreatedAt,
            property.ContactPhone,
            property.City,
            property.Address,
            property.Latitude,
            property.Longitude,
            property.ImageUrls
        );
    }

    public async Task<PropertyDetailDto> CreatePropertyAsync(PropertyCreateDto dto, CancellationToken cancellationToken = default)
    {
        double? lat = null;
        double? lon = null;

        // Auto geocode address via backend Nominatim service if Lat/Lon are missing
        if ((!lat.HasValue || !lon.HasValue) && !string.IsNullOrWhiteSpace(dto.Address))
        {
            var geoResult = await _geocodingService.GeocodeAddressAsync(dto.Address, dto.City, cancellationToken);
            if (geoResult.Success)
            {
                lat = geoResult.Latitude;
                lon = geoResult.Longitude;
            }
        }

        var property = new Property
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "UYU" : dto.Currency,
            Rooms = dto.Rooms,
            PropertyType = dto.PropertyType,
            OfferType = dto.OfferType,
            Status = PropertyStatus.Active,
            IsPremium = dto.IsPremium,
            CreatedAt = DateTime.UtcNow,
            ContactPhone = dto.ContactPhone,
            City = string.IsNullOrWhiteSpace(dto.City) ? _defaultCity : dto.City,
            Address = dto.Address,
            Latitude = lat,
            Longitude = lon,
            ImageUrls = dto.ImageUrls ?? new List<string>()
        };

        _dbContext.Properties.Add(property);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyDetailDto(
            property.Id,
            property.Title,
            property.Description,
            property.Price,
            property.Currency,
            property.Rooms,
            property.PropertyType,
            property.OfferType,
            property.Status,
            property.IsPremium,
            property.CreatedAt,
            property.ContactPhone,
            property.City,
            property.Address,
            property.Latitude,
            property.Longitude,
            property.ImageUrls
        );
    }

    public async Task<PropertyDetailDto?> UpdatePropertyAsync(Guid id, PropertyDetailDto dto, CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        if (property == null) return null;

        double? lat = dto.Latitude;
        double? lon = dto.Longitude;

        // Si cambia la dirección y no se envian coordenadas, recodificamos
        if ((!lat.HasValue || !lon.HasValue) && 
            !string.IsNullOrWhiteSpace(dto.Address) && 
            dto.Address != property.Address)
        {
            var geoResult = await _geocodingService.GeocodeAddressAsync(dto.Address, dto.City, cancellationToken);
            if (geoResult.Success)
            {
                lat = geoResult.Latitude;
                lon = geoResult.Longitude;
            }
        }

        // Actualizamos los campos
        property.Title = dto.Title;
        property.Description = dto.Description;
        property.Price = dto.Price;
        property.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "UYU" : dto.Currency;
        property.Rooms = dto.Rooms;
        property.PropertyType = dto.PropertyType;
        property.OfferType = dto.OfferType;
        property.IsPremium = dto.IsPremium;
        property.ContactPhone = dto.ContactPhone;
        property.City = string.IsNullOrWhiteSpace(dto.City) ? _defaultCity : dto.City;
        property.Address = dto.Address;
        property.Latitude = lat;
        property.Longitude = lon;
        property.ImageUrls = dto.ImageUrls ?? property.ImageUrls;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PropertyDetailDto(
            property.Id,
            property.Title,
            property.Description,
            property.Price,
            property.Currency,
            property.Rooms,
            property.PropertyType,
            property.OfferType,
            property.Status,
            property.IsPremium,
            property.CreatedAt,
            property.ContactPhone,
            property.City,
            property.Address,
            property.Latitude,
            property.Longitude,
            property.ImageUrls
        );
    }

    public async Task<bool> SoftDeletePropertyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (property == null) return false;

        property.IsDeleted = true;
        property.Status = PropertyStatus.Inactive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<bool> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        if (property == null) return false;

        // Si está activa, la pausa. Si está en cualquier otro estado, la activa.
        property.Status = property.Status == PropertyStatus.Active 
            ? PropertyStatus.Inactive 
            : PropertyStatus.Active;

        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return true;
    }

    public async Task<bool> RestorePropertyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id && p.IsDeleted, cancellationToken);

        if (property == null) return false;

        property.IsDeleted = false;
        property.Status = PropertyStatus.Inactive; // Se restaura como pausada por seguridad

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}