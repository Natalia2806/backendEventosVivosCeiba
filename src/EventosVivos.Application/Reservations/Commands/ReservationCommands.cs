using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Reservations.Commands;

public record CreateReservationCommand(
    int EventoId,
    int Cantidad,
    string NombreComprador,
    string EmailComprador) : IRequest<ReservaDto>;

public record ConfirmPaymentCommand(int ReservaId) : IRequest<ReservaDto>;

public record CancelReservationCommand(
    int ReservaId,
    string? EmailComprador = null,
    int? EventoId = null) : IRequest<ReservaDto>;
