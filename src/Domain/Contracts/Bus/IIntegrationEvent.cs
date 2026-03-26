namespace Domain.Contracts.Bus;

/// <summary>
///     Marker interface for integration events published via message bus.
///     All events intended for cross-boundary communication must implement this interface.
/// </summary>
public interface IIntegrationEvent;