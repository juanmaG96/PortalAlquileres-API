using System.ComponentModel.DataAnnotations;

namespace Marketplace.API.Data.Entities;

public class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    [Required]
    [MaxLength(10)]
    public string Currency { get; set; } = "UYU"; // UYU, USD

    public int Rooms { get; set; }

    public PropertyType PropertyType { get; set; }

    public OfferType OfferType { get; set; } = OfferType.Rent;

    public PropertyStatus Status { get; set; } = PropertyStatus.Active;

    public bool IsDeleted { get; set; } = false;

    public bool IsPremium { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [MaxLength(100)]
    public string City { get; set; } = "Paysandú";

    [MaxLength(250)]
    public string Address { get; set; } = string.Empty;

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}
