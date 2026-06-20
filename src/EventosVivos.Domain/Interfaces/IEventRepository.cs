using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Interfaces;

public interface IEventRepository
{
    Task<Evento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Evento>> GetAllAsync(
        EventType? tipo,
        DateTimeOffset? fechaDesde,
        DateTimeOffset? fechaHasta,
        int? venueId,
        EventStatus? estado,
        string? titulo,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Evento>> GetActiveOverlappingAsync(int venueId, DateTimeOffset inicio, DateTimeOffset fin, int? excludeEventId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Evento evento, CancellationToken cancellationToken = default);
    Task UpdateAsync(Evento evento, CancellationToken cancellationToken = default);
    Task<int> GetOccupiedTicketsAsync(int eventoId, CancellationToken cancellationToken = default);
}
