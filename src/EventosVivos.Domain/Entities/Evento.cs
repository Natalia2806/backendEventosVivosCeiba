using EventosVivos.Domain.Enums;

namespace EventosVivos.Domain.Entities;

public class Evento
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int VenueId { get; set; }
    public Venue Venue { get; set; } = null!;
    public int CapacidadMaxima { get; set; }
    public DateTimeOffset Inicio { get; set; }
    public DateTimeOffset Fin { get; set; }
    public decimal PrecioEntrada { get; set; }
    public EventType Tipo { get; set; }
    public EventStatus Estado { get; set; } = EventStatus.Activo;
    public ICollection<Reserva> Reservas { get; set; } = new List<Reserva>();
}
