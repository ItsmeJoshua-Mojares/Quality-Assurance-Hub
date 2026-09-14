using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using QAHub.Models;

namespace QAHub.ViewModels;

/// <summary>A snapshot row for the "Pass Rate by Run" list — rebuilt fresh on each Refresh().</summary>
public class RunReportRow
{
    public string Name { get; set; } = string.Empty;
    public string? BuildVersion { get; set; }
    public int PassRatePercent { get; set; }
    public int ExecutedCount { get; set; }
    public int TotalCount { get; set; }
    public string ProgressLabel => $"{ExecutedCount}/{TotalCount} executed";
}

public class ReportsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    private int _totalRuns;
    private int _totalExecutedCases;
    private string _overallPassRateLabel = "—";
    private int _totalRequirements;
    private int _fullyTestedCount;
    private int _partiallyTestedCount;
    private int _untestedCount;

    public ReportsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        RefreshCommand = new RelayCommand(_ => Refresh());
        Refresh();
    }

    public ObservableCollection<RunReportRow> RunRows { get; } = new();

    public int TotalRuns { get => _totalRuns; private set => SetProperty(ref _totalRuns, value); }
    public int TotalExecutedCases { get => _totalExecutedCases; private set => SetProperty(ref _totalExecutedCases, value); }
    public string OverallPassRateLabel { get => _overallPassRateLabel; private set => SetProperty(ref _overallPassRateLabel, value); }

    public int TotalRequirements { get => _totalRequirements; private set => SetProperty(ref _totalRequirements, value); }
    public int FullyTestedCount { get => _fullyTestedCount; private set => SetProperty(ref _fullyTestedCount, value); }
    public int PartiallyTestedCount { get => _partiallyTestedCount; private set => SetProperty(ref _partiallyTestedCount, value); }
    public int UntestedCount { get => _untestedCount; private set => SetProperty(ref _untestedCount, value); }

    // Percentages for the coverage stacked bar (0-100, based on TotalRequirements)
    public double FullyTestedPercent => TotalRequirements == 0 ? 0 : FullyTestedCount * 100.0 / TotalRequirements;
    public double PartiallyTestedPercent => TotalRequirements == 0 ? 0 : PartiallyTestedCount * 100.0 / TotalRequirements;
    public double UntestedPercent => TotalRequirements == 0 ? 0 : UntestedCount * 100.0 / TotalRequirements;

    public ICommand RefreshCommand { get; }

    /// <summary>Recomputes everything from MainViewModel.TestRuns and MainViewModel.Requirements.</summary>
    public void Refresh()
    {
        var runs = _mainViewModel.TestRuns.Runs;

        TotalRuns = runs.Count;
        TotalExecutedCases = runs.Sum(r => r.ExecutedCount);

        var totalPassed = runs.Sum(r => r.PassedCount);
        OverallPassRateLabel = TotalExecutedCases == 0 ? "—" : $"{System.Math.Round(totalPassed * 100.0 / TotalExecutedCases)}%";

        RunRows.Clear();
        foreach (var run in runs.OrderByDescending(r => r.CreatedAt))
        {
            RunRows.Add(new RunReportRow
            {
                Name = run.Name,
                BuildVersion = run.BuildVersion,
                PassRatePercent = run.PassRatePercent,
                ExecutedCount = run.ExecutedCount,
                TotalCount = run.TotalCount
            });
        }

        var requirements = _mainViewModel.Requirements.Requirements;
        TotalRequirements = requirements.Count;
        FullyTestedCount = requirements.Count(r => r.CoverageStatus == RequirementCoverageStatus.FullyTested);
        PartiallyTestedCount = requirements.Count(r => r.CoverageStatus == RequirementCoverageStatus.PartiallyTested);
        UntestedCount = requirements.Count(r => r.CoverageStatus == RequirementCoverageStatus.Untested);

        OnPropertyChanged(nameof(FullyTestedPercent));
        OnPropertyChanged(nameof(PartiallyTestedPercent));
        OnPropertyChanged(nameof(UntestedPercent));
    }
}
