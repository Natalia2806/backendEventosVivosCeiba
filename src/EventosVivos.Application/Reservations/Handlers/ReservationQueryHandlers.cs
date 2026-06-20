using EventosVivos.Application.DTOs;
using EventosVivos.Application.Mapping;
using EventosVivos.Application.Reservations.Queries;
using EventosVivos.Application.Venues.Queries;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Domain.Services;
using MediatR;

namespace EventosVivos.Application.Reservations.Handlers;

public class GetReservationsHandler(IReservationRepository reservationRepository)
    : IRequestHandler<GetReservationsQuery, IReadOnlyList<ReservaDto>>
{
    public async Task<IReadOnlyList<ReservaDto>> Handle(GetReservationsQuery request, CancellationToken cancellationToken)
    {
        var reservas = await reservationRepository.GetAllAsync(cancellationToken);
        return reservas.Select(EntityMapper.ToDto).ToList();
    }
}

public class GetOccupancyReportHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<GetOccupancyReportQuery, OccupancyReportDto>
{
    public async Task<OccupancyReportDto> Handle(GetOccupancyReportQuery request, CancellationToken cancellationToken)
    {
        var evento = await eventRepository.GetByIdAsync(request.EventoId, cancellationToken)
            ?? throw new NotFoundException($"Evento con id {request.EventoId} no encontrado.");

        var now = dateTimeProvider.UtcNow;
        var resolved = EventBusinessRules.ResolveStatus(evento, now);
        if (resolved == EventStatus.Completado && evento.Estado == EventStatus.Activo)
        {
            evento.Estado = EventStatus.Completado;
            await eventRepository.UpdateAsync(evento, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var vendidas = ReservationBusinessRules.CountSoldTickets(evento.Reservas);
        var occupied = ReservationBusinessRules.CountOccupiedTickets(evento.Reservas);
        var disponibles = evento.CapacidadMaxima - occupied;
        var ingresos = vendidas * evento.PrecioEntrada;

        return EntityMapper.ToOccupancyReport(evento, vendidas, disponibles, ingresos);
    }
}

public class GetVenuesHandler(IVenueRepository venueRepository)
    : IRequestHandler<GetVenuesQuery, IReadOnlyList<VenueDto>>
{
    public async Task<IReadOnlyList<VenueDto>> Handle(GetVenuesQuery request, CancellationToken cancellationToken)
    {
        var venues = await venueRepository.GetAllAsync(cancellationToken);
        return venues.Select(EntityMapper.ToDto).ToList();
    }
}
