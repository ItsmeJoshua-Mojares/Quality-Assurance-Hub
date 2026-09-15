using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using QAHub.Models;

namespace QAHub.ViewModels;

/// <summary>A single day's point for the homemade execution trend chart.</summary>
public class TrendDayLabel
{
    public string Label { get; set; } = string.Empty;
}

/// <summary>A row for the Recent Test Runs table.</summary>
public class RecentRunRow
{
    public string Name { get; set; } = string.Empty;
    public string? BuildVersion { get; set; }
    public TestRunStatus Status { get; set; }
    public int PassRatePercent { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>A row for the Recent Defects table.</summary>
public class RecentDefectRow
{
    public string Title { get; set; } = string.Empty;
    public BugStatus Status { get; set; }
    public DateTime OpenedDate { get; set; }
}

/// <summary>One wedge of the homemade Defect Severity donut chart, with its geometry pre-computed.</summary>
public class SeverityDonutSlice
{
    public Severity Severity { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public string PercentLabel { get; set; } = string.Empty;
    public Brush Fill { get; set; } = Brushes.Gray;
    public Geometry? Geometry { get; set; }
}

public class DashboardViewModel : ObservableObject
{
    private readonly MainViewModel _owner;

    // Fixed logical plot area size for the homemade trend chart (see TestExecutionTrendView.xaml usage)
    private const double ChartWidth = 460;
    private const double ChartHeight = 140;

    public DashboardViewModel(MainViewModel owner)
    {
        _owner = owner;

        CreateTestCaseCommand = new RelayCommand(_ => _owner.ShowTestCasesCommand.Execute(null));
        LogDefectCommand = new RelayCommand(_ => _owner.ShowBugsCommand.Execute(null));
        StartTestRunCommand = new RelayCommand(_ => _owner.ShowTestRunsCommand.Execute(null));
        ViewAllRunsCommand = new RelayCommand(_ => _owner.ShowTestRunsCommand.Execute(null));
        ViewAllDefectsCommand = new RelayCommand(_ => _owner.ShowBugsCommand.Execute(null));
    }

    public int TotalTestCases { get; private set; }
    public int PassedCases { get; private set; }
    public int FailedCases { get; private set; }
    public int DraftCases { get; private set; }
    public int BlockedCases { get; private set; }
    public int OpenBugs { get; private set; }
    public int TotalTestRuns { get; private set; }

    public string PassRate =>
        TotalTestCases == 0 ? "0%" : $"{Math.Round(PassedCases * 100.0 / TotalTestCases)}%";

    public string OpenBugsLabel => $"{OpenBugs} open";
    public IEnumerable<string> RecentBugs { get; private set; } = Enumerable.Empty<string>();

    // ---- Recent tables ----
    public ObservableCollection<RecentRunRow> RecentRuns { get; } = new();
    public ObservableCollection<RecentDefectRow> RecentDefects { get; } = new();

    // ---- Defect Severity donut (real data, built from Bug.Severity across open defects) ----
    public ObservableCollection<SeverityDonutSlice> DefectSeveritySlices { get; } = new();
    public int SeverityChartTotal { get; private set; }
    public bool HasDefectsForSeverityChart { get; private set; }

    // ---- Execution trend chart (real data, built from TestRunItem.ExecutedAt across all runs) ----
    public PointCollection PassedTrendPoints { get; private set; } = new();
    public PointCollection FailedTrendPoints { get; private set; } = new();
    public PointCollection BlockedTrendPoints { get; private set; } = new();
    public ObservableCollection<TrendDayLabel> TrendDayLabels { get; } = new();
    public bool HasTrendData { get; private set; }

    // ---- Quick action commands (pass-through to MainViewModel nav commands) ----
    public ICommand CreateTestCaseCommand { get; }
    public ICommand LogDefectCommand { get; }
    public ICommand StartTestRunCommand { get; }
    public ICommand ViewAllRunsCommand { get; }
    public ICommand ViewAllDefectsCommand { get; }

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

        RecentDefects.Clear();
        foreach (var bug in bugs.OrderByDescending(b => b.OpenedDate).Take(5))
        {
            RecentDefects.Add(new RecentDefectRow
            {
                Title = bug.Title,
                Status = bug.Status,
                OpenedDate = bug.OpenedDate
            });
        }

        var runs = _owner.TestRuns.Runs;
        TotalTestRuns = runs.Count;

        RecentRuns.Clear();
        foreach (var run in runs.OrderByDescending(r => r.CreatedAt).Take(5))
        {
            RecentRuns.Add(new RecentRunRow
            {
                Name = run.Name,
                BuildVersion = run.BuildVersion,
                Status = run.Status,
                PassRatePercent = run.PassRatePercent,
                CreatedAt = run.CreatedAt
            });
        }

        BuildExecutionTrend(runs);
        BuildDefectSeverityChart(bugs);

        OnPropertyChanged(nameof(TotalTestCases));
        OnPropertyChanged(nameof(PassedCases));
        OnPropertyChanged(nameof(FailedCases));
        OnPropertyChanged(nameof(DraftCases));
        OnPropertyChanged(nameof(BlockedCases));
        OnPropertyChanged(nameof(OpenBugs));
        OnPropertyChanged(nameof(TotalTestRuns));
        OnPropertyChanged(nameof(PassRate));
        OnPropertyChanged(nameof(OpenBugsLabel));
        OnPropertyChanged(nameof(RecentBugs));
        OnPropertyChanged(nameof(PassedTrendPoints));
        OnPropertyChanged(nameof(FailedTrendPoints));
        OnPropertyChanged(nameof(BlockedTrendPoints));
        OnPropertyChanged(nameof(HasTrendData));
        OnPropertyChanged(nameof(SeverityChartTotal));
        OnPropertyChanged(nameof(HasDefectsForSeverityChart));
    }

    /// <summary>
    /// Builds a 7-day execution trend from real TestRunItem.ExecutedAt timestamps
    /// across every run (no historical snapshots are stored — this reflects
    /// whatever items have been marked Pass/Fail/Blocked so far, bucketed by day).
    /// </summary>
    private void BuildExecutionTrend(IEnumerable<TestRun> runs)
    {
        var allItems = runs.SelectMany(r => r.Items)
            .Where(i => i.ExecutedAt.HasValue)
            .ToList();

        var days = Enumerable.Range(0, 7)
            .Select(offset => DateTime.Today.AddDays(-6 + offset))
            .ToList();

        TrendDayLabels.Clear();
        var passedCounts = new List<int>();
        var failedCounts = new List<int>();
        var blockedCounts = new List<int>();

        foreach (var day in days)
        {
            TrendDayLabels.Add(new TrendDayLabel { Label = day.ToString("MMM d") });

            var itemsThatDay = allItems.Where(i => i.ExecutedAt!.Value.Date == day.Date).ToList();
            passedCounts.Add(itemsThatDay.Count(i => i.Status == TestRunItemStatus.Passed));
            failedCounts.Add(itemsThatDay.Count(i => i.Status == TestRunItemStatus.Failed));
            blockedCounts.Add(itemsThatDay.Count(i => i.Status == TestRunItemStatus.Blocked));
        }

        HasTrendData = allItems.Count > 0;

        var maxValue = new[] { passedCounts.DefaultIfEmpty(0).Max(), failedCounts.DefaultIfEmpty(0).Max(), blockedCounts.DefaultIfEmpty(0).Max() }
            .Max();
        if (maxValue == 0) maxValue = 1;

        PassedTrendPoints = BuildPoints(passedCounts, maxValue);
        FailedTrendPoints = BuildPoints(failedCounts, maxValue);
        BlockedTrendPoints = BuildPoints(blockedCounts, maxValue);
    }

    private static PointCollection BuildPoints(List<int> values, int maxValue)
    {
        var points = new PointCollection();
        var count = values.Count;
        for (var i = 0; i < count; i++)
        {
            var x = count == 1 ? 0 : ChartWidth * i / (count - 1);
            var y = ChartHeight - (values[i] / (double)maxValue * ChartHeight);
            points.Add(new Point(x, y));
        }
        return points;
    }

    // Donut chart geometry constants (matches the Canvas size in DashboardView.xaml)
    private const double DonutCenter = 65;
    private const double DonutOuterRadius = 60;
    private const double DonutInnerRadius = 36;

    /// <summary>
    /// Builds the Defect Severity donut from real Bug.Severity values across
    /// open (non-Closed) defects — matching the same "open" filter used by
    /// the Open Defects stat card, so the two numbers stay consistent.
    /// </summary>
    private void BuildDefectSeverityChart(IEnumerable<Bug> bugs)
    {
        var openBugs = bugs.Where(b => b.Status != BugStatus.Closed).ToList();
        var total = openBugs.Count;

        SeverityChartTotal = total;
        HasDefectsForSeverityChart = total > 0;

        DefectSeveritySlices.Clear();
        if (total == 0) return;

        // Ordered least-severe to most-severe for a calm-to-alarming color ramp.
        var order = new[]
        {
            (Severity.Minor, "Minor", new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))),
            (Severity.Normal, "Normal", new SolidColorBrush(Color.FromRgb(0x2F, 0x81, 0xF7))),
            (Severity.Major, "Major", new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06))),
            (Severity.Critical, "Critical", new SolidColorBrush(Color.FromRgb(0xE2, 0x4C, 0x4C))),
        };

        double startAngle = 0;
        foreach (var (severity, label, brush) in order)
        {
            var count = openBugs.Count(b => b.Severity == severity);
            var percent = count * 100.0 / total;
            var sweep = count * 360.0 / total;
            var endAngle = startAngle + sweep;

            DefectSeveritySlices.Add(new SeverityDonutSlice
            {
                Severity = severity,
                Label = label,
                Count = count,
                PercentLabel = $"{Math.Round(percent)}%",
                Fill = brush,
                Geometry = count > 0
                    ? (count == total ? BuildFullDonutGeometry() : BuildDonutSliceGeometry(startAngle, endAngle))
                    : null
            });

            startAngle = endAngle;
        }
    }

    /// <summary>A full ring — used when one severity accounts for 100% of open defects,
    /// where a single 360° ArcSegment would be degenerate (start and end points coincide).</summary>
    private static Geometry BuildFullDonutGeometry()
    {
        var outerTop = PointOnCircle(DonutOuterRadius, 0);
        var outerBottom = PointOnCircle(DonutOuterRadius, 180);
        var innerTop = PointOnCircle(DonutInnerRadius, 0);
        var innerBottom = PointOnCircle(DonutInnerRadius, 180);

        var figure = new PathFigure { StartPoint = outerTop, IsClosed = true };
        figure.Segments.Add(new ArcSegment(outerBottom, new Size(DonutOuterRadius, DonutOuterRadius), 0, false, SweepDirection.Clockwise, true));
        figure.Segments.Add(new ArcSegment(outerTop, new Size(DonutOuterRadius, DonutOuterRadius), 0, false, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(innerTop, true));
        figure.Segments.Add(new ArcSegment(innerBottom, new Size(DonutInnerRadius, DonutInnerRadius), 0, false, SweepDirection.Counterclockwise, true));
        figure.Segments.Add(new ArcSegment(innerTop, new Size(DonutInnerRadius, DonutInnerRadius), 0, false, SweepDirection.Counterclockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    private static Geometry BuildDonutSliceGeometry(double startAngleDeg, double endAngleDeg)
    {
        var sweep = endAngleDeg - startAngleDeg;
        var isLargeArc = sweep > 180;

        var outerStart = PointOnCircle(DonutOuterRadius, startAngleDeg);
        var outerEnd = PointOnCircle(DonutOuterRadius, endAngleDeg);
        var innerStart = PointOnCircle(DonutInnerRadius, startAngleDeg);
        var innerEnd = PointOnCircle(DonutInnerRadius, endAngleDeg);

        var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };
        figure.Segments.Add(new ArcSegment(outerEnd, new Size(DonutOuterRadius, DonutOuterRadius),
            0, isLargeArc, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(innerEnd, true));
        figure.Segments.Add(new ArcSegment(innerStart, new Size(DonutInnerRadius, DonutInnerRadius),
            0, isLargeArc, SweepDirection.Counterclockwise, true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    /// <summary>Point on a circle of the given radius, centered on the donut, at a clock-style angle (0° = top, clockwise).</summary>
    private static Point PointOnCircle(double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Point(
            DonutCenter + radius * Math.Sin(radians),
            DonutCenter - radius * Math.Cos(radians));
    }
}
