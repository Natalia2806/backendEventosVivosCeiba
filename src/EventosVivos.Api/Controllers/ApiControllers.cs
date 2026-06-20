using EventosVivos.Application.Events.Commands;
using EventosVivos.Application.Events.Queries;
using EventosVivos.Application.Reservations.Commands;using EventosVivos.Application.Reservations.Queries;
using EventosVivos.Application.Venues.Queries;
using EventosVivos.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EventosVivos.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VenuesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetVenuesQuery(), cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class EventsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EventType>(request.Tipo, true, out var tipo))
            return BadRequest(new ProblemDetails { Title = "Tipo de evento inválido", Detail = "Valores válidos: conferencia, taller, concierto" });

        var command = new CreateEventCommand(
            request.Titulo,
            request.Descripcion,
            request.VenueId,
            request.CapacidadMaxima,
            request.Inicio,
            request.Fin,
            request.PrecioEntrada,
            tipo);

        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? tipo,
        [FromQuery] DateTimeOffset? fechaDesde,
        [FromQuery] DateTimeOffset? fechaHasta,
        [FromQuery] int? venueId,
        [FromQuery] string? estado,
        [FromQuery] string? titulo,
        CancellationToken cancellationToken)
    {
        EventType? eventType = null;
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            if (!Enum.TryParse<EventType>(tipo, true, out var parsed))
                return BadRequest(new ProblemDetails { Title = "Tipo inválido" });
            eventType = parsed;
        }

        EventStatus? eventStatus = null;
        if (!string.IsNullOrWhiteSpace(estado))
        {
            if (!Enum.TryParse<EventStatus>(estado, true, out var parsed))
                return BadRequest(new ProblemDetails { Title = "Estado inválido" });
            eventStatus = parsed;
        }

        var result = await mediator.Send(new GetEventsQuery(
            eventType, fechaDesde, fechaHasta, venueId, eventStatus, titulo), cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEventByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/reservations")]
    public async Task<IActionResult> CreateReservation(int id, [FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateReservationCommand(id, request.Cantidad, request.NombreComprador, request.EmailComprador);
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction("GetById", "Reservations", new { id = result.Id }, result);
    }

    [HttpGet("{id:int}/occupancy-report")]
    public async Task<IActionResult> GetOccupancyReport(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOccupancyReportQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelEventCommand(id), cancellationToken);
        return Ok(result);
    }
}

[ApiController]
[Route("api/[controller]")]
public class ReservationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetReservationsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var all = await mediator.Send(new GetReservationsQuery(), cancellationToken);
        var reserva = all.FirstOrDefault(r => r.Id == id);
        return reserva is null ? NotFound() : Ok(reserva);
    }

    [HttpPost("{id:int}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ConfirmPaymentCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelReservationRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CancelReservationCommand(id, request?.EmailComprador, request?.EventoId),
            cancellationToken);
        return Ok(result);
    }
}

public record CancelReservationRequest(string? EmailComprador, int? EventoId);

public record CreateEventRequest(
    string Titulo,
    string Descripcion,
    int VenueId,
    int CapacidadMaxima,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    decimal PrecioEntrada,
    string Tipo);

public record CreateReservationRequest(
    int Cantidad,
    string NombreComprador,
    string EmailComprador);
