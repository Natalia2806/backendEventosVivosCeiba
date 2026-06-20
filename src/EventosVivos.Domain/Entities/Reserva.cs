using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Entities;

public class Reserva
{
    public int Id { get; set; }
    public int EventoId { get; set; }
    public Evento Evento { get; set; } = null!;
    public int Cantidad { get; set; }
    public string NombreComprador { get; set; } = string.Empty;
    public string EmailComprador { get; set; } = string.Empty;
    public ReservationStatus Estado { get; set; } = ReservationStatus.PendientePago;
    public string? CodigoReserva { get; set; }
    public DateTimeOffset? FechaCancelacion { get; set; }
    public DateTimeOffset FechaCreacion { get; set; }
}
