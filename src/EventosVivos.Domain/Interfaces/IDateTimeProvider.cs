namespace EventosVivos.Domain.Interfaces;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
