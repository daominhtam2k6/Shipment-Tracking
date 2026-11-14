using Tracking.Contracts;

namespace Tracking.Api;

public enum AddEventOutcome { Success, NotFound, InvalidTransition }

public sealed record AddEventResult(AddEventOutcome Outcome, ShipmentEventResponse? Event = null);

public interface IShipmentRepository
{
    Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request);
    Task<ShipmentResponse?> GetAsync(string trackingCode);
    Task<AddEventResult> AddEventAsync(string trackingCode, AddShipmentEventRequest request);
    Task<IReadOnlyList<ShipmentEventResponse>?> GetHistoryAsync(string trackingCode);
}

public sealed class InMemoryShipmentRepository : IShipmentRepository
{
    private sealed class Entry(ShipmentResponse shipment)
    {
        public ShipmentResponse Shipment { get; set; } = shipment;
        public List<ShipmentEventResponse> Events { get; } = [];
    }

    private readonly Dictionary<string, Entry> _items = new();
    private readonly object _lock = new();

    public Task<ShipmentResponse> CreateAsync(CreateShipmentRequest request)
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;

            string trackingCode;
            do
            {
                trackingCode = TrackingCodeGenerator.Generate(now);
            } while (_items.ContainsKey(trackingCode));

            var location = !string.IsNullOrWhiteSpace(request.InitialLocation)
                ? request.InitialLocation.Trim()
                : "Chưa xác định";

            var shipment = new ShipmentResponse(
                TrackingCode: trackingCode,
                SenderName: request.SenderName.Trim(),
                SenderAddress: request.SenderAddress.Trim(),
                ReceiverName: request.ReceiverName.Trim(),
                ReceiverAddress: request.ReceiverAddress.Trim(),
                Status: ShipmentStatus.Created,
                CurrentLocation: location,
                CreatedAt: now);

            var entry = new Entry(shipment);
            entry.Events.Add(new ShipmentEventResponse(
                EventId: Guid.NewGuid(),
                TrackingCode: trackingCode,
                Status: ShipmentStatus.Created,
                Location: location,
                Note: null,
                OccurredAt: now));

            _items[trackingCode] = entry;

            return Task.FromResult(shipment);
        }
    }

    public Task<ShipmentResponse?> GetAsync(string trackingCode)
    {
        lock (_lock)
        {
            return Task.FromResult<ShipmentResponse?>(
                _items.TryGetValue(trackingCode, out var entry) ? entry.Shipment : null);
        }
    }

    public Task<AddEventResult> AddEventAsync(string trackingCode, AddShipmentEventRequest request)
    {
        lock (_lock)
        {
            if (!_items.TryGetValue(trackingCode, out var entry))
                return Task.FromResult(new AddEventResult(AddEventOutcome.NotFound));

            if (!ShipmentStateMachine.CanMove(entry.Shipment.Status, request.Status))
                return Task.FromResult(new AddEventResult(AddEventOutcome.InvalidTransition));

            var location = request.Location.Trim();

            var shipmentEvent = new ShipmentEventResponse(
                EventId: Guid.NewGuid(),
                TrackingCode: trackingCode,
                Status: request.Status,
                Location: location,
                Note: request.Note?.Trim(),
                OccurredAt: DateTimeOffset.UtcNow);

            entry.Events.Add(shipmentEvent);
            entry.Shipment = entry.Shipment with
            {
                Status = request.Status,
                CurrentLocation = location
            };

            return Task.FromResult(new AddEventResult(AddEventOutcome.Success, shipmentEvent));
        }
    }

    public Task<IReadOnlyList<ShipmentEventResponse>?> GetHistoryAsync(string trackingCode)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<ShipmentEventResponse>?>(
                _items.TryGetValue(trackingCode, out var entry) ? entry.Events.ToList() : null);
        }
    }
}