namespace Tracking.Contracts;

// Tracking.Api -> Notification.Api -> Customer.Client
public sealed record ShipmentNotification(
    Guid EventId,
    string TrackingCode,
    ShipmentStatus Status,
    string Location,
    DateTimeOffset OccurredAt);

public static class ShipmentHubContract
{
    public const string Path = "/hubs/shipments";              // địa chỉ hub
    public const string StatusChanged = "ShipmentStatusChanged"; // server -> client
    public const string Watch = "WatchShipment";                 // client -> server
    public const string Unwatch = "UnwatchShipment";             // client -> server

    public static string GroupName(string normalizedCode) => $"shipment:{normalizedCode}";
}