using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Interfaces;
using EventosVivos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EventosVivos.Infrastructure.Repositories;

public class ReservationRepository(AppDbContext context) : IReservationRepository
{
    public async Task<Reserva?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await context.Reservas
            .Include(r => r.Evento)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reserva>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reservas = await context.Reservas
            .Include(r => r.Evento)
            .ToListAsync(cancellationToken);

        return reservas
            .OrderByDescending(r => r.FechaCreacion)
            .ToList();
    }

    public async Task AddAsync(Reserva reserva, CancellationToken cancellationToken = default)
    {
        await context.Reservas.AddAsync(reserva, cancellationToken);
    }

    public Task UpdateAsync(Reserva reserva, CancellationToken cancellationToken = default)
    {
        context.Reservas.Update(reserva);
        return Task.CompletedTask;
    }

    public async Task<bool> CodigoExistsAsync(string codigo, CancellationToken cancellationToken = default) =>
        await context.Reservas.AnyAsync(r => r.CodigoReserva == codigo, cancellationToken);

    public async Task<int> GetNextCodigoSequenceAsync(CancellationToken cancellationToken = default)
    {
        var seq = await context.CodigoSequences.FirstAsync(cancellationToken);
        seq.LastValue++;
        context.CodigoSequences.Update(seq);
        await context.SaveChangesAsync(cancellationToken);
        return seq.LastValue;
    }
}
