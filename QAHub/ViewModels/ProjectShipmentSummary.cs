using QAHub.Models;

namespace QAHub.ViewModels;

/// <summary>Dashboard row pairing a Project with the count of shipments/serials
/// shipped under it.</summary>
public class ProjectShipmentSummary
{
    public Project Project { get; init; } = null!;

    public int ShipmentCount { get; init; }
}