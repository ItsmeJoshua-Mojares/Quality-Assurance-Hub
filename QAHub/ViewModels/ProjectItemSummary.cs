using QAHub.Models;

namespace QAHub.ViewModels;

/// <summary>Dashboard row pairing a Project with the count of RMA/CAPAR items
/// filed under it. Project is null for the synthetic "Unassigned" row.</summary>
public class ProjectItemSummary
{
    public Project? Project { get; init; }

    public int ItemCount { get; init; }

    public string ProjectName => Project?.Name ?? "Unassigned";

    public string StatusLabel => Project?.Status.ToString() ?? "—";

    public bool IsUnassigned => Project == null;
}