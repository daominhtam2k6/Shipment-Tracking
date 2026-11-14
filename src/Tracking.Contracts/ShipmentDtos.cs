namespace Tracking.Contracts;

public sealed record CreateShipmentRequest (
    string SenderName,
    string SenderAddress,
    string ReceiverName,
    string ReceiverAddress,
    string? InitialLocation = null
);
public sealed record ShipmentResponse(
    string TrackingCode,
    string SenderName,
    string SenderAddress,
    string ReceiverName,
    string ReceiverAddress,
    ShipmentStatus Status,
    string CurrentLocation,
    DateTimeOffset CreatedAt
);
public sealed record AddShipmentEventRequest(
    ShipmentStatus Status,
    string Location,
    string? Note = null);
public sealed record ShipmentEventResponse(
    Guid EventId,
    string TrackingCode,
    ShipmentStatus Status,
    string Location,
    string? Note,
    DateTimeOffset OccurredAt
);
public sealed record ApiError(string Code, string Message)
{
    public const string InvalidShipment = "INVALID_SHIPMENT";
    public const string InvalidEvent = "INVALID_EVENT";
    public const string ShipmentNotFound = "SHIPMENT_NOT_FOUND";
    public const string InvalidTransition = "INVALID_TRANSITION";
}