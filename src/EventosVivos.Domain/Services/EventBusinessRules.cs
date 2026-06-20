using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Services;

public static class EventBusinessRules
{
    private static readonly TimeZoneInfo BogotaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

    public static void ValidateVenueCapacity(Evento evento, Venue venue)
    {
        if (evento.CapacidadMaxima > venue.Capacidad)
            throw new BusinessRuleException($"La capacidad máxima ({evento.CapacidadMaxima}) no puede exceder la del venue ({venue.Capacidad}).");
    }

    public static void ValidateNoOverlap(IReadOnlyList<Evento> overlappingEvents)
    {
        if (overlappingEvents.Count > 0)
            throw new ConflictException("Existe un conflicto de horarios con otro evento activo en el mismo venue.");
    }

    public static void ValidateWeekendNightRestriction(DateTimeOffset inicio)
    {
        var local = TimeZoneInfo.ConvertTime(inicio, BogotaTimeZone);
        if (local.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday && local.TimeOfDay > new TimeSpan(22, 0, 0))
            throw new BusinessRuleException("Los eventos en fin de semana no pueden iniciar después de las 22:00 (hora Bogotá).");
    }

    public static void ValidateFutureStart(DateTimeOffset inicio, DateTimeOffset now)
    {
        if (inicio <= now)
            throw new BusinessRuleException("La fecha y hora de inicio debe ser futura.");
    }

    public static void ValidateEndAfterStart(DateTimeOffset inicio, DateTimeOffset fin)
    {
        if (fin <= inicio)
            throw new BusinessRuleException("La fecha y hora de fin debe ser posterior al inicio.");
    }

    public static EventStatus ResolveStatus(Evento evento, DateTimeOffset now)
    {
        if (evento.Estado == EventStatus.Cancelado)
            return EventStatus.Cancelado;

        if (now > evento.Fin)
            return EventStatus.Completado;

        return evento.Estado;
    }

    public static void ValidateCanCancel(Evento evento, DateTimeOffset now)
    {
        var status = ResolveStatus(evento, now);
        if (status == EventStatus.Cancelado)
            throw new ConflictException("El evento ya está cancelado.");

        if (status == EventStatus.Completado)
            throw new ConflictException("No se puede cancelar un evento completado.");
    }
}
