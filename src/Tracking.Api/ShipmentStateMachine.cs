using Tracking.Contracts;

namespace Tracking.Api;

public static class ShipmentStateMachine
{
    public static bool CanMove(ShipmentStatus from, ShipmentStatus to) =>
        from switch
        {
            ShipmentStatus.Created => to == ShipmentStatus.InTransit,
            ShipmentStatus.InTransit => to is ShipmentStatus.AtHub
                or ShipmentStatus.Delivered or ShipmentStatus.Failed,
            ShipmentStatus.AtHub => to is ShipmentStatus.InTransit
                or ShipmentStatus.Delivered or ShipmentStatus.Failed,
            ShipmentStatus.Failed => to == ShipmentStatus.InTransit,
            _ => false
        };
}