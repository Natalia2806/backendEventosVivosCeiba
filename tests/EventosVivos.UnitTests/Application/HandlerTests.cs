using EventosVivos.Application.Events.Commands;
using EventosVivos.Application.Events.Handlers;
using EventosVivos.Application.Reservations.Commands;
using EventosVivos.Application.Reservations.Handlers;
using EventosVivos.Application.Reservations.Queries;
using EventosVivos.Domain.Entities;
using EventosVivos.Domain.Enums;
using EventosVivos.Domain.Exceptions;
using EventosVivos.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace EventosVivos.UnitTests.Application;

public class CreateEventHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateEvent_WhenDataIsValid()
    {
        var now = DateTimeOffset.UtcNow;
        var inicio = now.AddDays(10);
        var fin = inicio.AddHours(2);
        var venue = new Venue { Id = 1, Nombre = "Teatro Central", Capacidad = 500, Ciudad = "Bogotá" };

        var eventRepository = new Mock<IEventRepository>();
        eventRepository
            .Setup(r => r.GetActiveOverlappingAsync(1, inicio, fin, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Evento>());
        eventRepository
            .Setup(r => r.AddAsync(It.IsAny<Evento>(), It.IsAny<CancellationToken>()))
            .Callback<Evento, CancellationToken>((evento, _) => evento.Id = 7)
            .Returns(Task.CompletedTask);

        var venueRepository = new Mock<IVenueRepository>();
        venueRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(venue);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(p => p.UtcNow).Returns(now);

        var handler = new CreateEventHandler(
            eventRepository.Object,
            venueRepository.Object,
            unitOfWork.Object,
            dateTimeProvider.Object);

        var command = new CreateEventCommand(
            "Conferencia de Arquitectura",
            "Descripción suficientemente larga para validar.",
            1,
            200,
            inicio,
            fin,
            75m,
            EventType.Conferencia);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().Be(7);
        result.Titulo.Should().Be(command.Titulo);
        result.Tipo.Should().Be("conferencia");
        result.Estado.Should().Be("activo");
        eventRepository.Verify(r => r.AddAsync(It.IsAny<Evento>(), It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class ConfirmPaymentHandlerTests
{
    [Fact]
    public async Task Handle_ShouldConfirmPayment_AndAssignReservationCode()
    {
        var reserva = new Reserva
        {
            Id = 5,
            EventoId = 1,
            Cantidad = 2,
            NombreComprador = "Ana",
            EmailComprador = "ana@test.com",
            Estado = ReservationStatus.PendientePago,
            FechaCreacion = DateTimeOffset.UtcNow
        };

        var reservationRepository = new Mock<IReservationRepository>();
        reservationRepository
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        reservationRepository
            .Setup(r => r.GetNextCodigoSequenceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        reservationRepository
            .Setup(r => r.CodigoExistsAsync("EV-000042", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        reservationRepository
            .Setup(r => r.UpdateAsync(reserva, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new ConfirmPaymentHandler(reservationRepository.Object, unitOfWork.Object);

        var result = await handler.Handle(new ConfirmPaymentCommand(5), CancellationToken.None);

        result.Estado.Should().Be("confirmada");
        result.CodigoReserva.Should().Be("EV-000042");
        reservationRepository.Verify(r => r.UpdateAsync(reserva, It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class CancelReservationHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCancelReservation_WhenBuyerMatches()
    {
        var now = DateTimeOffset.UtcNow;
        var evento = new Evento
        {
            Id = 1,
            Titulo = "Taller Angular",
            Inicio = now.AddDays(5),
            Fin = now.AddDays(5).AddHours(2),
            Estado = EventStatus.Activo,
            CapacidadMaxima = 100,
            PrecioEntrada = 50
        };
        var reserva = new Reserva
        {
            Id = 9,
            EventoId = 1,
            Evento = evento,
            Cantidad = 1,
            EmailComprador = "comprador@test.com",
            Estado = ReservationStatus.Confirmada,
            FechaCreacion = now
        };

        var eventRepository = new Mock<IEventRepository>();
        eventRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evento);

        var reservationRepository = new Mock<IReservationRepository>();
        reservationRepository
            .Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        reservationRepository
            .Setup(r => r.UpdateAsync(reserva, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(p => p.UtcNow).Returns(now);

        var handler = new CancelReservationHandler(
            eventRepository.Object,
            reservationRepository.Object,
            unitOfWork.Object,
            dateTimeProvider.Object);

        var result = await handler.Handle(
            new CancelReservationCommand(9, "comprador@test.com", 1),
            CancellationToken.None);

        result.Estado.Should().Be("cancelada");
        result.FechaCancelacion.Should().NotBeNull();
    }
}

public class GetReservationByIdHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnReservation_WhenExists()
    {
        var reserva = new Reserva
        {
            Id = 3,
            EventoId = 1,
            Evento = new Evento { Titulo = "Concierto Rock" },
            Cantidad = 2,
            NombreComprador = "Luis",
            EmailComprador = "luis@test.com",
            Estado = ReservationStatus.PendientePago,
            FechaCreacion = DateTimeOffset.UtcNow
        };

        var reservationRepository = new Mock<IReservationRepository>();
        reservationRepository
            .Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        var handler = new GetReservationByIdHandler(reservationRepository.Object);

        var result = await handler.Handle(new GetReservationByIdQuery(3), CancellationToken.None);

        result.Id.Should().Be(3);
        result.EventoTitulo.Should().Be("Concierto Rock");
        result.Estado.Should().Be("pendientepago");
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenReservationDoesNotExist()
    {
        var reservationRepository = new Mock<IReservationRepository>();
        reservationRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reserva?)null);

        var handler = new GetReservationByIdHandler(reservationRepository.Object);

        var act = () => handler.Handle(new GetReservationByIdQuery(999), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
