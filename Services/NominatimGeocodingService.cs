using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marketplace.API.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace Marketplace.API.Services;

public class NominatimGeocodingService : INominatimGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NominatimGeocodingService> _logger;
    private readonly string _defaultCity;
    private readonly string _defaultProvince;
    private readonly string _defaultCountry;

    // Strict Throttling: 1 request per second for Nominatim API policy compliance
    private static readonly SemaphoreSlim Throttler = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;

    public NominatimGeocodingService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<NominatimGeocodingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _defaultCity = configuration["WhiteLabelSettings:InstanceCity"] ?? "Paysandú";
        _defaultProvince = configuration["WhiteLabelSettings:InstanceProvince"] ?? "Paysandú";
        _defaultCountry = configuration["WhiteLabelSettings:InstanceCountry"] ?? "Uruguay";

        string userAgent = configuration["GeocodingSettings:UserAgent"] 
            ?? "PaysanduRealEstateMarketplace/1.0 (contact@alquilerespaysandu.com)";

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", userAgent);
        }
    }

    public async Task<GeocodeResultDto> GeocodeAddressAsync(string address, string? city = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return new GeocodeResultDto(string.Empty, 0, 0, false, "La dirección no puede estar vacía.");
        }

        string targetCity = string.IsNullOrWhiteSpace(city) ? _defaultCity : city;
        string fullQuery = $"{address}, {targetCity}, {_defaultProvince}, {_defaultCountry}".ToLowerInvariant().Trim();
        string cacheKey = $"geocode_{fullQuery}";

        // 1. Check MemoryCache first
        if (_cache.TryGetValue(cacheKey, out GeocodeResultDto? cachedResult) && cachedResult != null)
        {
            _logger.LogInformation("Geocoding cache hit for query: {Query}", fullQuery);
            return cachedResult;
        }

        // 2. Enforce Throttling (Max 1 request / second)
        await Throttler.WaitAsync(cancellationToken);
        try
        {
            TimeSpan elapsed = DateTime.UtcNow - _lastRequestTime;
            if (elapsed < TimeSpan.FromSeconds(1))
            {
                TimeSpan delayNeeded = TimeSpan.FromSeconds(1) - elapsed;
                _logger.LogDebug("Throttling Nominatim request: Waiting {Ms}ms", delayNeeded.TotalMilliseconds);
                await Task.Delay(delayNeeded, cancellationToken);
            }

            _lastRequestTime = DateTime.UtcNow;

            // 3. Query OpenStreetMap Nominatim API
            string url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(fullQuery)}&format=json&limit=1&addressdetails=1";
            _logger.LogInformation("Calling Nominatim API: {Url}", url);

            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Nominatim API returned HTTP status {StatusCode}", response.StatusCode);
                return new GeocodeResultDto(address, 0, 0, false, $"Error en servicio externo de geocodificación ({response.StatusCode}).");
            }

            string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<List<NominatimResponse>>(jsonContent);

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("Geocoding found no results for address: {Query}", fullQuery);
                var notFoundResult = new GeocodeResultDto(address, 0, 0, false, "No se encontraron coordenadas para la dirección especificada.");
                // Cache negative response for 30 minutes to prevent repeated failing external calls
                _cache.Set(cacheKey, notFoundResult, TimeSpan.FromMinutes(30));
                return notFoundResult;
            }

            var first = results[0];
            if (double.TryParse(first.Lat, CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(first.Lon, CultureInfo.InvariantCulture, out double lon))
            {
                var successResult = new GeocodeResultDto(
                    first.DisplayName ?? $"{address}, {targetCity}",
                    lat,
                    lon,
                    true,
                    null
                );

                // Cache successful geocoding result for 24 hours
                _cache.Set(cacheKey, successResult, TimeSpan.FromHours(24));
                return successResult;
            }

            return new GeocodeResultDto(address, 0, 0, false, "Error al interpretar coordenadas del servicio.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error geocoding address: {Address}", address);
            return new GeocodeResultDto(address, 0, 0, false, $"Excepción en el servicio: {ex.Message}");
        }
        finally
        {
            Throttler.Release();
        }
    }

    private class NominatimResponse
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
