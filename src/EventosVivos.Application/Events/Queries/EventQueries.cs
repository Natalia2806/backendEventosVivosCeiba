using EventosVivos.Application.DTOs;
using EventosVivos.Domain.Enums;
using MediatR;

namespace EventosVivos.Application.Events.Queries;

public record GetEventsQuery(
    EventType? Tipo,
    DateTimeOffset? FechaDesde,
    DateTimeOffset? FechaHasta,
    int? VenueId,
    EventStatus? Estado,
    string? Titulo) : IRequest<IReadOnlyList<EventoDto>>;

public record GetEventByIdQuery(int Id) : IRequest<EventoDto>;
