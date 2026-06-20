using EventosVivos.Application.DTOs;
using EventosVivos.Application.Events.Queries;
using EventosVivos.Application.Mapping;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Domain.Services;
using MediatR;

namespace EventosVivos.Application.Events.Handlers;

public class GetEventsHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<GetEventsQuery, IReadOnlyList<EventoDto>>
{
    public async Task<IReadOnlyList<EventoDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var eventos = await eventRepository.GetAllAsync(
            request.Tipo,
            request.FechaDesde,
            request.FechaHasta,
            request.VenueId,
            request.Estado,
            request.Titulo,
            cancellationToken);

        var now = dateTimeProvider.UtcNow;
        var changed = false;

        foreach (var evento in eventos)
        {
            var resolved = EventBusinessRules.ResolveStatus(evento, now);
            if (resolved == EventStatus.Completado && evento.Estado == EventStatus.Activo)
            {
                evento.Estado = EventStatus.Completado;
                await eventRepository.UpdateAsync(evento, cancellationToken);
                changed = true;
            }
        }

        if (changed)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new List<EventoDto>();
        foreach (var evento in eventos)
        {
            var occupied = await eventRepository.GetOccupiedTicketsAsync(evento.Id, cancellationToken);
            result.Add(EntityMapper.ToDto(evento, evento.CapacidadMaxima - occupied));
        }

        return result;
    }
}

public class GetEventByIdHandler(
    IEventRepository eventRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<GetEventByIdQuery, EventoDto>
{
    public async Task<EventoDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var evento = await eventRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Evento con id {request.Id} no encontrado.");

        var now = dateTimeProvider.UtcNow;
        var resolved = EventBusinessRules.ResolveStatus(evento, now);
        if (resolved == EventStatus.Completado && evento.Estado == EventStatus.Activo)
        {
            evento.Estado = EventStatus.Completado;
            await eventRepository.UpdateAsync(evento, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var occupied = await eventRepository.GetOccupiedTicketsAsync(evento.Id, cancellationToken);
        return EntityMapper.ToDto(evento, evento.CapacidadMaxima - occupied);
    }
}
