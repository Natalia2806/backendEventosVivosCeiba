using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Events.Commands;

public record CancelEventCommand(int EventoId) : IRequest<EventoDto>;
