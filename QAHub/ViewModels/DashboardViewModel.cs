using QAHub.Models;

namespace QAHub.ViewModels;

public class DashboardViewModel : ObservableObject
{
    private readonly MainViewModel _owner;

    public DashboardViewModel(MainViewModel owner)
    {
        _owner = owner;
    }

    public int TotalTestCases { get; private set; }
    public int PassedCases { get; private set; }
    public int FailedCases { get; private set; }
    public int DraftCases { get; private set; }
    public int BlockedCases { get; private set; }
    public int OpenBugs { get; private set; }

    public string PassRate =>
        TotalTestCases == 0 ? "0%" : $"{Math.Round(PassedCases * 100.0 / TotalTestCases)}%";

    public string OpenBugsLabel => $"{OpenBugs} open";
    public IEnumerable<string> RecentBugs { get; private set; } = Enumerable.Empty<string>();

    public void Refresh()
    {
        var cases = _owner.TestCases.Items;
        TotalTestCases = cases.Count;
        PassedCases = cases.Count(tc => tc.Status == TestStatus.Passed);
        FailedCases = cases.Count(tc => tc.Status == TestStatus.Failed);
        BlockedCases = cases.Count(tc => tc.Status == TestStatus.Blocked);
        DraftCases = cases.Count(tc => tc.Status == TestStatus.Draft);

        var bugs = _owner.Bugs.Items;
        OpenBugs = bugs.Count(b => b.Status != BugStatus.Closed);
        RecentBugs = bugs
            .Where(b => b.Status != BugStatus.Closed)
            .OrderByDescending(b => b.OpenedDate)
            .Take(5)
            .Select(b => b.Title)
            .ToList();

        OnPropertyChanged(nameof(TotalTestCases));
        OnPropertyChanged(nameof(PassedCases));
        OnPropertyChanged(nameof(FailedCases));
        OnPropertyChanged(nameof(DraftCases));
        OnPropertyChanged(nameof(BlockedCases));
        OnPropertyChanged(nameof(OpenBugs));
        OnPropertyChanged(nameof(PassRate));
        OnPropertyChanged(nameof(OpenBugsLabel));
        OnPropertyChanged(nameof(RecentBugs));
    }
}