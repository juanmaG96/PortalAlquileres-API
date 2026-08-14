using FluentValidation;
using Marketplace.API.Dtos;

namespace Marketplace.API.Validators;

/// <summary>
/// Validador para la actualización de propiedades (extiende la validación de creación e incluye validación de ID).
/// </summary>
public class PropertyUpdateDtoValidator : AbstractValidator<PropertyDetailDto>
{
    public PropertyUpdateDtoValidator()
    {
        Include(new PropertyCreateDtoValidator());

        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID de la propiedad es obligatorio para actualizar.");
    }
}
