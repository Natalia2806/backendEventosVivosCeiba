using EventosVivos.Application.DTOs;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Services;

namespace EventosVivos.Application.Mapping;

public static class EntityMapper
{
    public static EventoDto ToDto(Evento evento, int entradasDisponibles) => new(
        evento.Id,
        evento.Titulo,
        evento.Descripcion,
        evento.VenueId,
        evento.Venue?.Nombre ?? string.Empty,
        evento.CapacidadMaxima,
        evento.Inicio,
        evento.Fin,
        evento.PrecioEntrada,
        evento.Tipo.ToString().ToLowerInvariant(),
        evento.Estado.ToString().ToLowerInvariant(),
        entradasDisponibles);

    public static ReservaDto ToDto(Reserva reserva) => new(
        reserva.Id,
        reserva.EventoId,
        reserva.Evento?.Titulo ?? string.Empty,
        reserva.Cantidad,
        reserva.NombreComprador,
        reserva.EmailComprador,
        reserva.Estado.ToString().ToLowerInvariant(),
        reserva.CodigoReserva,
        reserva.FechaCancelacion,
        reserva.FechaCreacion);

    public static VenueDto ToDto(Venue venue) => new(
        venue.Id,
        venue.Nombre,
        venue.Capacidad,
        venue.Ciudad);

    public static OccupancyReportDto ToOccupancyReport(Evento evento, int vendidas, int disponibles, decimal ingresos) =>
        new(
            evento.Id,
            evento.Titulo,
            vendidas,
            disponibles,
            evento.CapacidadMaxima == 0 ? 0 : Math.Round((decimal)vendidas / evento.CapacidadMaxima * 100, 2),
            ingresos,
            evento.Estado.ToString().ToLowerInvariant());
}
