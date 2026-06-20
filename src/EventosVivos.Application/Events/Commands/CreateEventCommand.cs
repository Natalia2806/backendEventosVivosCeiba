using EventosVivos.Application.DTOs;
using EventosVivos.Domain.Enums;
using MediatR;

namespace EventosVivos.Application.Events.Commands;

public record CreateEventCommand(
    string Titulo,
    string Descripcion,
    int VenueId,
    int CapacidadMaxima,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    decimal PrecioEntrada,
    EventType Tipo) : IRequest<EventoDto>;
