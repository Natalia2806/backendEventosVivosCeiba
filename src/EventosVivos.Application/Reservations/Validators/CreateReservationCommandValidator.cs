using EventosVivos.Application.Reservations.Commands;
using FluentValidation;

namespace EventosVivos.Application.Reservations.Validators;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.EventoId)
            .GreaterThan(0);

        RuleFor(x => x.Cantidad)
            .GreaterThanOrEqualTo(1).WithMessage("La cantidad debe ser 1 o más.");

        RuleFor(x => x.NombreComprador)
            .NotEmpty().WithMessage("El nombre del comprador es obligatorio.");

        RuleFor(x => x.EmailComprador)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El email no tiene un formato válido.");
    }
}
