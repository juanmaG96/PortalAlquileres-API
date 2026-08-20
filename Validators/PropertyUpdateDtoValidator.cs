using FluentValidation;
using Marketplace.API.Dtos;

namespace Marketplace.API.Validators;

/// <summary>
/// Validador para la actualización de propiedades.
/// </summary>
public class PropertyUpdateDtoValidator : AbstractValidator<PropertyDetailDto>
{
    public PropertyUpdateDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la propiedad es obligatorio para actualizar.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .Length(10, 150).WithMessage("El título debe tener entre 10 y 150 caracteres.");

        RuleFor(x => x.Price)
            .NotNull().WithMessage("El precio es obligatorio.")
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

        RuleFor(x => x.Rooms)
            .GreaterThanOrEqualTo(0).WithMessage("Las habitaciones deben ser mayor o igual a 0.");

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("El teléfono de contacto es obligatorio.")
            .Matches(@"^[\d\s\+\-]{8,15}$")
            .WithMessage("El formato del teléfono no es válido (solo números, espacios, '+' o '-', entre 8 y 15 caracteres).");

        RuleFor(x => x.PropertyType)
            .IsInEnum().WithMessage("La categoría (PropertyType) seleccionada no es válida.");

        RuleFor(x => x.OfferType)
            .IsInEnum().WithMessage("El tipo de oferta (OfferType) no es válido.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("La ciudad es obligatoria.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("La dirección es obligatoria.");
    }
}