using Microsoft.AspNetCore.SignalR;
using Tracking.Contracts;

namespace Notification.Api;

public sealed class ShipmentHub : Hub
{
    public async Task WatchShipment(string trackingCode)
    {
        if(!TrackingCodeGenerator.TryNormalize(trackingCode, out var code))
            throw new HubException("Mã theo dõi không hợp lệ.");
        await Groups.AddToGroupAsync(Context.ConnectionId, ShipmentHubContract.GroupName(code));
    }

    public async Task UnwatchShipment(string trackingCode)
    {
        if (!TrackingCodeGenerator.TryNormalize(trackingCode, out var code))
            throw new HubException("Mã theo dõi không hợp lệ.");

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ShipmentHubContract.GroupName(code));
    }
}