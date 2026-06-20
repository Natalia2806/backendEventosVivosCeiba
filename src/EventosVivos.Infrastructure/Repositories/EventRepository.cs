using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public class EventRepository(AppDbContext context) : IEventRepository
{
    public async Task<Evento?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Eventos
            .Include(e => e.Venue)
            .Include(e => e.Reservas)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Evento>> GetAllAsync(
        EventType? tipo,
        DateTimeOffset? fechaDesde,
        DateTimeOffset? fechaHasta,
        int? venueId,
        EventStatus? estado,
        string? titulo,
        CancellationToken cancellationToken = default)
    {
        var query = context.Eventos.Include(e => e.Venue).AsQueryable();

        if (venueId.HasValue)
            query = query.Where(e => e.VenueId == venueId.Value);

        var results = await query.ToListAsync(cancellationToken);

        if (fechaDesde.HasValue)
            results = results.Where(e => e.Inicio >= fechaDesde.Value).ToList();

        if (fechaHasta.HasValue)
            results = results.Where(e => e.Inicio <= fechaHasta.Value).ToList();

        if (tipo.HasValue)
            results = results.Where(e => e.Tipo == tipo.Value).ToList();

        if (estado.HasValue)
            results = results.Where(e => e.Estado == estado.Value).ToList();

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            var term = titulo.ToLowerInvariant();
            results = results.Where(e => e.Titulo.ToLowerInvariant().Contains(term)).ToList();
        }

        return results.OrderBy(e => e.Inicio).ToList();
    }

    public async Task<IReadOnlyList<Evento>> GetActiveOverlappingAsync(
        int venueId,
        DateTimeOffset inicio,
        DateTimeOffset fin,
        int? excludeEventId = null,
        CancellationToken cancellationToken = default)
    {
        var events = await context.Eventos
            .Where(e => e.VenueId == venueId)
            .ToListAsync(cancellationToken);

        if (excludeEventId.HasValue)
            events = events.Where(e => e.Id != excludeEventId.Value).ToList();

        return events
            .Where(e => e.Estado == EventStatus.Activo && e.Inicio < fin && e.Fin > inicio)
            .ToList();
    }

    public async Task AddAsync(Evento evento, CancellationToken cancellationToken = default)
    {
        await context.Eventos.AddAsync(evento, cancellationToken);
    }

    public Task UpdateAsync(Evento evento, CancellationToken cancellationToken = default)
    {
        context.Eventos.Update(evento);
        return Task.CompletedTask;
    }

    public async Task<int> GetOccupiedTicketsAsync(int eventoId, CancellationToken cancellationToken = default)
    {
        var reservas = await context.Reservas
            .Where(r => r.EventoId == eventoId)
            .ToListAsync(cancellationToken);

        return reservas
            .Where(r => r.Estado is ReservationStatus.PendientePago or ReservationStatus.Confirmada or ReservationStatus.Perdida)
            .Sum(r => r.Cantidad);
    }
}
