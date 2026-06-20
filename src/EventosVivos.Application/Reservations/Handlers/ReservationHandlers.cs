using EventosVivos.Application.DTOs;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Reservations.Commands;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Domain.Services;
using MediatR;

namespace EventosVivos.Application.Reservations.Handlers;

public class CreateReservationHandler(
    IEventRepository eventRepository,
    IReservationRepository reservationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateReservationCommand, ReservaDto>
{
    public async Task<ReservaDto> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var evento = await eventRepository.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException($"Evento con id {request.EventoId} no encontrado.");

        var now = dateTimeProvider.UtcNow;
        var resolved = EventBusinessRules.ResolveStatus(evento, now);
        if (resolved == EventStatus.Completado && evento.Estado == EventStatus.Activo)
        {
            evento.Estado = EventStatus.Completado;
            await eventRepository.UpdateAsync(evento, cancellationToken);
        }

        ReservationBusinessRules.ValidateEventAllowsReservations(evento, now);
        ReservationBusinessRules.ValidateLateReservationRestriction(evento, now);
        ReservationBusinessRules.ValidateQuantityLimits(evento, request.Cantidad, now);

        var occupied = await eventRepository.GetOccupiedTicketsAsync(evento.Id, cancellationToken);
        var disponibles = evento.CapacidadMaxima - occupied;
        ReservationBusinessRules.ValidateAvailability(request.Cantidad, disponibles);

        var reserva = new Reserva
        {
            EventoId = request.EventoId,
            Cantidad = request.Cantidad,
            NombreComprador = request.NombreComprador,
            EmailComprador = request.EmailComprador,
            Estado = ReservationStatus.PendientePago,
            FechaCreacion = now
        };

        await reservationRepository.AddAsync(reserva, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        reserva.Evento = evento;
        return EntityMapper.ToDto(reserva);
    }
}

public class ConfirmPaymentHandler(
    IReservationRepository reservationRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmPaymentCommand, ReservaDto>
{
    public async Task<ReservaDto> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var reserva = await reservationRepository.GetByIdAsync(request.ReservaId, cancellationToken)
            ?? throw new NotFoundException($"Reserva con id {request.ReservaId} no encontrada.");

        ReservationBusinessRules.ValidateCanConfirm(reserva);

        string codigo;
        do
        {
            var seq = await reservationRepository.GetNextCodigoSequenceAsync(cancellationToken);
            codigo = $"EV-{seq:D6}";
        } while (await reservationRepository.CodigoExistsAsync(codigo, cancellationToken));

        reserva.Estado = ReservationStatus.Confirmada;
        reserva.CodigoReserva = codigo;

        await reservationRepository.UpdateAsync(reserva, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EntityMapper.ToDto(reserva);
    }
}

public class CancelReservationHandler(
    IEventRepository eventRepository,
    IReservationRepository reservationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CancelReservationCommand, ReservaDto>
{
    public async Task<ReservaDto> Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var reserva = await reservationRepository.GetByIdAsync(request.ReservaId, cancellationToken)
            ?? throw new NotFoundException($"Reserva con id {request.ReservaId} no encontrada.");

        ReservationBusinessRules.ValidateCancelOwnership(reserva, request.EmailComprador);

        if (request.EventoId.HasValue && reserva.EventoId != request.EventoId.Value)
            throw new BusinessRuleException("La reserva no pertenece a este evento.");

        ReservationBusinessRules.ValidateCanCancel(reserva);

        var evento = await eventRepository.GetByIdAsync(reserva.EventoId, cancellationToken)
            ?? throw new NotFoundException($"Evento con id {reserva.EventoId} no encontrado.");

        var now = dateTimeProvider.UtcNow;
        ReservationBusinessRules.ValidateEventAllowsReservationCancel(evento, now);
        reserva.Estado = ReservationBusinessRules.ResolveCancellationStatus(reserva, evento, now);
        reserva.FechaCancelacion = now;

        await reservationRepository.UpdateAsync(reserva, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EntityMapper.ToDto(reserva);
    }
}
