using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;

namespace EventosVivos.Domain.Services;

public static class ReservationBusinessRules
{
    public static void ValidateLateReservationRestriction(Evento evento, DateTimeOffset now)
    {
        if (evento.Inicio - now < TimeSpan.FromHours(1))
            throw new BusinessRuleException("No se permiten reservas para eventos que inicien en menos de 1 hora.");
    }

    public static void ValidateQuantityLimits(Evento evento, int cantidad, DateTimeOffset now)
    {
        if (cantidad < 1)
            throw new BusinessRuleException("La cantidad debe ser 1 o más.");

        if (evento.PrecioEntrada > 100 && cantidad > 10)
            throw new BusinessRuleException("Eventos con precio mayor a $100 limitan a máximo 10 entradas por transacción.");

        if (evento.Inicio - now < TimeSpan.FromHours(24) && cantidad > 5)
            throw new BusinessRuleException("Con menos de 24 horas para el evento, solo se permiten máximo 5 entradas por transacción.");
    }

    public static void ValidateAvailability(int cantidad, int disponibles)
    {
        if (cantidad > disponibles)
            throw new BusinessRuleException($"No hay suficientes entradas disponibles. Disponibles: {disponibles}.");
    }

    public static void ValidateEventAllowsReservations(Evento evento, DateTimeOffset now)
    {
        var status = EventBusinessRules.ResolveStatus(evento, now);
        if (status != EventStatus.Activo)
            throw new BusinessRuleException("No se pueden realizar reservas para eventos que no están activos.");
    }

    public static void ValidateCanConfirm(Reserva reserva)
    {
        if (reserva.Estado == ReservationStatus.Confirmada)
            throw new ConflictException("La reserva ya está confirmada.");

        if (reserva.Estado == ReservationStatus.Cancelada)
            throw new ConflictException("No se puede confirmar una reserva cancelada.");

        if (reserva.Estado == ReservationStatus.CancelacionEvento)
            throw new ConflictException("No se puede confirmar una reserva cancelada por cancelación del evento.");

        if (reserva.Estado == ReservationStatus.Perdida)
            throw new ConflictException("No se puede confirmar una reserva perdida.");

        if (reserva.Estado != ReservationStatus.PendientePago)
            throw new ConflictException("Solo se pueden confirmar reservas en estado pendiente de pago.");
    }

    public static void ValidateCanCancel(Reserva reserva)
    {
        if (reserva.Estado == ReservationStatus.Cancelada)
            throw new ConflictException("La reserva ya está cancelada.");

        if (reserva.Estado == ReservationStatus.CancelacionEvento)
            throw new ConflictException("La reserva fue cancelada por cancelación del evento.");

        if (reserva.Estado == ReservationStatus.Perdida)
            throw new ConflictException("La reserva ya fue registrada como perdida.");

        if (reserva.Estado is not (ReservationStatus.PendientePago or ReservationStatus.Confirmada))
            throw new ConflictException("La reserva no puede ser cancelada en su estado actual.");
    }

    public static void ValidateCancelOwnership(Reserva reserva, string? emailComprador)
    {
        if (string.IsNullOrWhiteSpace(emailComprador))
            return;

        if (!string.Equals(reserva.EmailComprador, emailComprador.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("El email no coincide con la reserva.");
    }

    public static void ValidateEventAllowsReservationCancel(Evento evento, DateTimeOffset now)
    {
        var status = EventBusinessRules.ResolveStatus(evento, now);
        if (status is EventStatus.Cancelado or EventStatus.Completado)
            throw new BusinessRuleException("No se pueden cancelar reservas de un evento cancelado o finalizado.");
    }

    public static ReservationStatus ResolveCancellationStatus(Reserva reserva, Evento evento, DateTimeOffset now)
    {
        if (reserva.Estado == ReservationStatus.Confirmada &&
            evento.Inicio - now < TimeSpan.FromHours(48))
            return ReservationStatus.Perdida;

        return ReservationStatus.Cancelada;
    }

    public static int CountOccupiedTickets(IEnumerable<Reserva> reservas) =>
        reservas
            .Where(r => r.Estado is ReservationStatus.PendientePago or ReservationStatus.Confirmada or ReservationStatus.Perdida)
            .Sum(r => r.Cantidad);

    public static int CountSoldTickets(IEnumerable<Reserva> reservas) =>
        reservas
            .Where(r => r.Estado is ReservationStatus.Confirmada or ReservationStatus.Perdida)
            .Sum(r => r.Cantidad);
}
