namespace EventosVivos.Application.DTOs;

public record VenueDto(int Id, string Nombre, int Capacidad, string Ciudad);

public record EventoDto(
    int Id,
    string Titulo,
    string Descripcion,
    int VenueId,
    string VenueNombre,
    int CapacidadMaxima,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    decimal PrecioEntrada,
    string Tipo,
    string Estado,
    int EntradasDisponibles);

public record ReservaDto(
    int Id,
    int EventoId,
    string EventoTitulo,
    int Cantidad,
    string NombreComprador,
    string EmailComprador,
    string Estado,
    string? CodigoReserva,
    DateTimeOffset? FechaCancelacion,
    DateTimeOffset FechaCreacion);

public record OccupancyReportDto(
    int EventoId,
    string Titulo,
    int TotalEntradasVendidas,
    int EntradasDisponibles,
    decimal PorcentajeOcupacion,
    decimal TotalIngresos,
    string Estado);
