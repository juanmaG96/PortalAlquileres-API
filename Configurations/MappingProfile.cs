using AutoMapper;
using Marketplace.API.Data.Entities;
using Marketplace.API.Dtos;

namespace Marketplace.API.Configurations;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Entity -> DTO Mappings
        CreateMap<Property, PropertySummaryDto>()
            .ForCtorParam(nameof(PropertySummaryDto.CoverImageUrl),
                opt => opt.MapFrom(src => src.ImageUrls.FirstOrDefault()));

        CreateMap<Property, PropertyDetailDto>();

        // DTO -> Entity Mappings
        CreateMap<PropertyCreateDto, Property>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Latitude, opt => opt.Ignore())
            .ForMember(dest => dest.Longitude, opt => opt.Ignore());

        CreateMap<PropertyDetailDto, Property>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
    }
}
