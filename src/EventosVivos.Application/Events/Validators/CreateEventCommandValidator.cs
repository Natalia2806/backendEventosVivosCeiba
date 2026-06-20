using EventosVivos.Application.Events.Commands;
using FluentValidation;

namespace EventosVivos.Application.Events.Validators;

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .Length(5, 100).WithMessage("El título debe tener entre 5 y 100 caracteres.");

        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria.")
            .Length(10, 500).WithMessage("La descripción debe tener entre 10 y 500 caracteres.");

        RuleFor(x => x.VenueId)
            .GreaterThan(0).WithMessage("El venue es obligatorio.");

        RuleFor(x => x.CapacidadMaxima)
            .GreaterThan(0).WithMessage("La capacidad máxima debe ser un entero positivo.");

        RuleFor(x => x.Inicio)
            .NotEmpty().WithMessage("La fecha de inicio es obligatoria.");

        RuleFor(x => x.Fin)
            .NotEmpty().WithMessage("La fecha de fin es obligatoria.")
            .GreaterThan(x => x.Inicio).WithMessage("La fecha de fin debe ser posterior al inicio.");

        RuleFor(x => x.PrecioEntrada)
            .GreaterThan(0).WithMessage("El precio de entrada debe ser positivo.");

        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de evento no es válido.");
    }
}
