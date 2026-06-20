using EventosVivos.Domain.Entities;

namespace EventosVivos.Domain.Interfaces;

public interface IReservationRepository
{
    Task<Reserva?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reserva>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Reserva reserva, CancellationToken cancellationToken = default);
    Task UpdateAsync(Reserva reserva, CancellationToken cancellationToken = default);
    Task<bool> CodigoExistsAsync(string codigo, CancellationToken cancellationToken = default);
    Task<int> GetNextCodigoSequenceAsync(CancellationToken cancellationToken = default);
}
