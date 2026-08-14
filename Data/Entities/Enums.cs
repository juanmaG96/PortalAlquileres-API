namespace Marketplace.API.Data.Entities;

public enum PropertyType
{
    House = 1,
    Apartment = 2,
    Commercial = 3,
    Land = 4,
    Room = 5,
    Residence = 6
}

public enum OfferType
{
    Rent = 1,      // Oferta (En Alquiler)
    Demand = 2     // Demanda (Busco Alquiler)
}

public enum PropertyStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2,
    Archived = 3
}
