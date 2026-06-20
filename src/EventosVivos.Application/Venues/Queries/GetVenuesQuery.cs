using EventosVivos.Application.DTOs;
using MediatR;

namespace EventosVivos.Application.Venues.Queries;

public record GetVenuesQuery : IRequest<IReadOnlyList<VenueDto>>;
