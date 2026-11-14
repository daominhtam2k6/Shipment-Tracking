namespace Tracking.Contracts;

public static class ShipmentStateMachine
{
    private static readonly Dictionary<ShipmentStatus, ShipmentStatus[]> Allowed = new Dictionary<ShipmentStatus, ShipmentStatus[]>()
    {
        [ShipmentStatus.Created] = [ShipmentStatus.InTransit],
        [ShipmentStatus.InTransit] = [ShipmentStatus.AtHub, ShipmentStatus.Delivered, ShipmentStatus.Failed],
        [ShipmentStatus.AtHub] = [ShipmentStatus.InTransit, ShipmentStatus.Delivered, ShipmentStatus.Failed],
        [ShipmentStatus.Failed] = [ShipmentStatus.InTransit],
        [ShipmentStatus.Delivered] = []
    };
    public static bool CanMove(ShipmentStatus fromState, ShipmentStatus toState) => Allowed[fromState].Contains(toState);

}