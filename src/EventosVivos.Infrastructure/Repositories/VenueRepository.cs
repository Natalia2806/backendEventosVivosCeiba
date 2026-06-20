using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public class VenueRepository(AppDbContext context) : IVenueRepository
{
    public async Task<Venue?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Venues.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.Venues.OrderBy(v => v.Id).ToListAsync(cancellationToken);
}
