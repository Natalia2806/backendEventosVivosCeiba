using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Reservations.Queries;

public record GetReservationsQuery : IRequest<IReadOnlyList<ReservaDto>>;

public record GetOccupancyReportQuery(int EventoId) : IRequest<OccupancyReportDto>;
