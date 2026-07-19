namespace Ayoos.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
