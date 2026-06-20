using EventosVivos.Application.DTOs;
using EventosVivos.Application.Events.Commands;
using EventosVivos.Application.Mapping;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Domain.Services;
using MediatR;

namespace EventosVivos.Application.Events.Handlers;

public class CreateEventHandler(
    IEventRepository eventRepository,
    IVenueRepository venueRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateEventCommand, EventoDto>
{
    public async Task<EventoDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        EventBusinessRules.ValidateFutureStart(request.Inicio, now);
        EventBusinessRules.ValidateEndAfterStart(request.Inicio, request.Fin);
        EventBusinessRules.ValidateWeekendNightRestriction(request.Inicio);

        var venue = await venueRepository.GetByIdAsync(request.VenueId, cancellationToken)
            ?? throw new NotFoundException($"Venue con id {request.VenueId} no encontrado.");

        var evento = new Evento
        {
            Titulo = request.Titulo,
            Descripcion = request.Descripcion,
            VenueId = request.VenueId,
            CapacidadMaxima = request.CapacidadMaxima,
            Inicio = request.Inicio,
            Fin = request.Fin,
            PrecioEntrada = request.PrecioEntrada,
            Tipo = request.Tipo,
            Estado = EventStatus.Activo,
            Venue = venue
        };

        EventBusinessRules.ValidateVenueCapacity(evento, venue);

        var overlapping = await eventRepository.GetActiveOverlappingAsync(
            request.VenueId, request.Inicio, request.Fin, cancellationToken: cancellationToken);
        EventBusinessRules.ValidateNoOverlap(overlapping);

        await eventRepository.AddAsync(evento, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EntityMapper.ToDto(evento, evento.CapacidadMaxima);
    }
}
