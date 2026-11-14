using Tracking.Contracts;

namespace Tracking.Api.Tests;

public class ShipmentStateMachineTests
{
    private static readonly Dictionary<ShipmentStatus, ShipmentStatus[]> Expected = new()
    {
        [ShipmentStatus.Created]   = [ShipmentStatus.InTransit],
        [ShipmentStatus.InTransit] = [ShipmentStatus.AtHub, ShipmentStatus.Delivered, ShipmentStatus.Failed],
        [ShipmentStatus.AtHub]     = [ShipmentStatus.InTransit, ShipmentStatus.Delivered, ShipmentStatus.Failed],
        [ShipmentStatus.Delivered] = [],
        [ShipmentStatus.Failed]    = [ShipmentStatus.InTransit]
    };

    public static TheoryData<ShipmentStatus, ShipmentStatus, bool> AllPairs()
    {
        var data = new TheoryData<ShipmentStatus, ShipmentStatus, bool>();
        foreach (var fromState in Enum.GetValues<ShipmentStatus>())
            foreach (var toState in Enum.GetValues<ShipmentStatus>())
                data.Add(fromState, toState, Expected[fromState].Contains(toState));
        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void CanMove_MatchesBusinessTable(ShipmentStatus fromState, ShipmentStatus toState, bool expected)
    {
        Assert.Equal(expected, ShipmentStateMachine.CanMove(fromState, toState));
    }

    [Fact]
    public void CanMove_UndefinedFromState_Throws()
    {
        Assert.Throws<KeyNotFoundException>(
            () => ShipmentStateMachine.CanMove((ShipmentStatus)99, ShipmentStatus.InTransit));
    }

    [Fact]
    public void CanMove_UndefinedToState_ReturnsFalse()
    {
        Assert.False(ShipmentStateMachine.CanMove(ShipmentStatus.InTransit, (ShipmentStatus)99));
    }
}