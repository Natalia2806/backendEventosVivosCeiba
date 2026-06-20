using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Services;
using FluentAssertions;

namespace EventosVivos.UnitTests.Domain;

public class EventBusinessRulesTests
{
    private static Venue CreateVenue(int capacidad = 200) =>
        new() { Id = 1, Nombre = "Test", Capacidad = capacidad, Ciudad = "Bogotá" };

    private static Evento CreateEvento(int capacidadMaxima = 100) =>
        new()
        {
            CapacidadMaxima = capacidadMaxima,
            VenueId = 1,
            Venue = CreateVenue()
        };

    [Fact]
    public void RN01_ShouldReject_WhenCapacityExceedsVenue()
    {
        var evento = CreateEvento(250);
        var act = () => EventBusinessRules.ValidateVenueCapacity(evento, CreateVenue(200));
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RN01_ShouldPass_WhenCapacityWithinVenue()
    {
        var evento = CreateEvento(200);
        var act = () => EventBusinessRules.ValidateVenueCapacity(evento, CreateVenue(200));
        act.Should().NotThrow();
    }

    [Fact]
    public void RN02_ShouldReject_WhenOverlappingEventsExist()
    {
        var act = () => EventBusinessRules.ValidateNoOverlap([CreateEvento()]);
        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void RN03_ShouldReject_WeekendAfter22()
    {
        // Sábado 20-jun-2026 23:00 Bogotá (UTC-5) = 21-jun-2026 04:00 UTC
        var saturday23Bogota = new DateTimeOffset(2026, 6, 21, 4, 0, 0, TimeSpan.Zero);
        var act = () => EventBusinessRules.ValidateWeekendNightRestriction(saturday23Bogota);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RN06_ShouldMarkCompleted_WhenEndDatePassed()
    {
        var evento = new Evento
        {
            Estado = EventStatus.Activo,
            Fin = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var status = EventBusinessRules.ResolveStatus(evento, DateTimeOffset.UtcNow);
        status.Should().Be(EventStatus.Completado);
    }
}

public class ReservationBusinessRulesTests
{
    private static Evento CreateFutureEvent(decimal price = 50, int hoursUntilStart = 72) =>
        new()
        {
            Inicio = DateTimeOffset.UtcNow.AddHours(hoursUntilStart),
            PrecioEntrada = price,
            Estado = EventStatus.Activo,
            CapacidadMaxima = 100
        };

    [Fact]
    public void RN04_ShouldReject_WhenLessThan1Hour()
    {
        var evento = CreateFutureEvent(hoursUntilStart: 0);
        evento.Inicio = DateTimeOffset.UtcNow.AddMinutes(30);
        var act = () => ReservationBusinessRules.ValidateLateReservationRestriction(evento, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RN05_ShouldReject_WhenMoreThan10TicketsForExpensiveEvent()
    {
        var evento = CreateFutureEvent(price: 150);
        var act = () => ReservationBusinessRules.ValidateQuantityLimits(evento, 11, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RF03_ShouldReject_WhenMoreThan5TicketsWithin24Hours()
    {
        var evento = CreateFutureEvent();
        evento.Inicio = DateTimeOffset.UtcNow.AddHours(12);
        var act = () => ReservationBusinessRules.ValidateQuantityLimits(evento, 6, DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void RN07_ShouldReturnPerdida_WhenConfirmedCancelledWithin48Hours()
    {
        var evento = CreateFutureEvent();
        evento.Inicio = DateTimeOffset.UtcNow.AddHours(24);
        var reserva = new Reserva { Estado = ReservationStatus.Confirmada, Cantidad = 2 };
        var status = ReservationBusinessRules.ResolveCancellationStatus(reserva, evento, DateTimeOffset.UtcNow);
        status.Should().Be(ReservationStatus.Perdida);
    }

    [Fact]
    public void RN07_ShouldReturnCancelada_WhenConfirmedCancelledWithMoreThan48Hours()
    {
        var evento = CreateFutureEvent(hoursUntilStart: 100);
        var reserva = new Reserva { Estado = ReservationStatus.Confirmada, Cantidad = 2 };
        var status = ReservationBusinessRules.ResolveCancellationStatus(reserva, evento, DateTimeOffset.UtcNow);
        status.Should().Be(ReservationStatus.Cancelada);
    }

    [Fact]
    public void PerdidaTickets_ShouldCountAsOccupiedButNotAvailable()
    {
        var reservas = new List<Reserva>
        {
            new() { Cantidad = 5, Estado = ReservationStatus.Confirmada },
            new() { Cantidad = 3, Estado = ReservationStatus.Perdida },
            new() { Cantidad = 2, Estado = ReservationStatus.Cancelada }
        };
        ReservationBusinessRules.CountOccupiedTickets(reservas).Should().Be(8);
        ReservationBusinessRules.CountSoldTickets(reservas).Should().Be(8);
    }

    [Fact]
    public void CancelacionEvento_ShouldNotCountAsOccupied()
    {
        var reservas = new List<Reserva>
        {
            new() { Cantidad = 4, Estado = ReservationStatus.CancelacionEvento },
            new() { Cantidad = 2, Estado = ReservationStatus.Confirmada }
        };
        ReservationBusinessRules.CountOccupiedTickets(reservas).Should().Be(2);
    }

    [Fact]
    public void ValidateCanCancel_ShouldReject_AlreadyCancelledEvent()
    {
        var evento = new Evento { Estado = EventStatus.Cancelado, Fin = DateTimeOffset.UtcNow.AddDays(1) };
        var act = () => EventBusinessRules.ValidateCanCancel(evento, DateTimeOffset.UtcNow);
        act.Should().Throw<ConflictException>();
    }

    [Fact]
    public void ValidateCanCancel_ShouldReject_CompletedEvent()
    {
        var evento = new Evento
        {
            Estado = EventStatus.Activo,
            Fin = DateTimeOffset.UtcNow.AddHours(-1)
        };
        var act = () => EventBusinessRules.ValidateCanCancel(evento, DateTimeOffset.UtcNow);
        act.Should().Throw<ConflictException>();
    }
}
