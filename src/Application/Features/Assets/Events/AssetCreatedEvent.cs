using Domain.Contracts.Bus;

namespace Application.Features.Assets.Events;

public record AssetCreatedEvent : IIntegrationEvent
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public DateTime AcquisitionDate { get; init; }
    public string? Category { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}