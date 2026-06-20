using EventosVivos.Application.DTOs;
using EventosVivos.Application.Events.Commands;
using EventosVivos.Application.Mapping;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Domain.Services;
using MediatR;

namespace EventosVivos.Application.Events.Handlers;

public class CancelEventHandler(
    IEventRepository eventRepository,
    IReservationRepository reservationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CancelEventCommand, EventoDto>
{
    public async Task<EventoDto> Handle(CancelEventCommand request, CancellationToken cancellationToken)
    {
        var evento = await eventRepository.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException($"Evento con id {request.EventoId} no encontrado.");

        var now = dateTimeProvider.UtcNow;
        EventBusinessRules.ValidateCanCancel(evento, now);

        evento.Estado = EventStatus.Cancelado;

        foreach (var reserva in evento.Reservas.Where(r =>
                     r.Estado is ReservationStatus.PendientePago or ReservationStatus.Confirmada))
        {
            reserva.Estado = ReservationStatus.CancelacionEvento;
            reserva.FechaCancelacion = now;
            await reservationRepository.UpdateAsync(reserva, cancellationToken);
        }

        await eventRepository.UpdateAsync(evento, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var occupied = ReservationBusinessRules.CountOccupiedTickets(evento.Reservas);
        var disponibles = evento.CapacidadMaxima - occupied;
        return EntityMapper.ToDto(evento, disponibles);
    }
}
