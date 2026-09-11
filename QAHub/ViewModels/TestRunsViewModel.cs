using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using QAHub.Models;

namespace QAHub.ViewModels;

public class TestRunsViewModel : ObservableObject
{
    private TestRun? _selectedRun;
    private string _newRunName = string.Empty;
    private string? _newRunBuildVersion;
    private string _newItemCaseName = string.Empty;

    public TestRunsViewModel()
    {
        CreateRunCommand = new RelayCommand(_ => CreateRun(), _ => !string.IsNullOrWhiteSpace(NewRunName));
        OpenRunCommand = new RelayCommand(param => { if (param is TestRun run) OpenRun(run); });
        BackToListCommand = new RelayCommand(_ => SelectedRun = null);
        DeleteRunCommand = new RelayCommand(param => { if (param is TestRun run) DeleteRun(run); });

        AddItemCommand = new RelayCommand(_ => AddItem(), _ => SelectedRun != null && !string.IsNullOrWhiteSpace(NewItemCaseName));
        RemoveItemCommand = new RelayCommand(param => { if (param is TestRunItem item) SelectedRun?.Items.Remove(item); });

        MarkPassedCommand = new RelayCommand(param => SetStatus(param, TestRunItemStatus.Passed));
        MarkFailedCommand = new RelayCommand(param => SetStatus(param, TestRunItemStatus.Failed));
        MarkBlockedCommand = new RelayCommand(param => SetStatus(param, TestRunItemStatus.Blocked));
        MarkSkippedCommand = new RelayCommand(param => SetStatus(param, TestRunItemStatus.Skipped));

        ReportBugCommand = new RelayCommand(param => ReportBug(param as TestRunItem));
        CompleteRunCommand = new RelayCommand(_ => CompleteRun(), _ => SelectedRun != null && SelectedRun.Status != TestRunStatus.Completed);
    }

    public ObservableCollection<TestRun> Runs { get; } = new();

    public TestRun? SelectedRun
    {
        get => _selectedRun;
        private set
        {
            if (SetProperty(ref _selectedRun, value))
            {
                OnPropertyChanged(nameof(IsDetailView));
                OnPropertyChanged(nameof(IsListView));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsDetailView => SelectedRun != null;
    public bool IsListView => SelectedRun == null;

    public string NewRunName
    {
        get => _newRunName;
        set { SetProperty(ref _newRunName, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string? NewRunBuildVersion
    {
        get => _newRunBuildVersion;
        set => SetProperty(ref _newRunBuildVersion, value);
    }

    public string NewItemCaseName
    {
        get => _newItemCaseName;
        set { SetProperty(ref _newItemCaseName, value); CommandManager.InvalidateRequerySuggested(); }
    }

    // ---- Overview stat cards (across all runs) ----
    public int TotalRuns => Runs.Count;
    public int InProgressRuns => Runs.Count(r => r.Status == TestRunStatus.InProgress);
    public int CompletedRuns => Runs.Count(r => r.Status == TestRunStatus.Completed);

    public string OverallPassRateLabel
    {
        get
        {
            var totalExecuted = Runs.Sum(r => r.ExecutedCount);
            if (totalExecuted == 0) return "—";
            var totalPassed = Runs.Sum(r => r.PassedCount);
            return $"{Math.Round(totalPassed * 100.0 / totalExecuted)}%";
        }
    }

    public ICommand CreateRunCommand { get; }
    public ICommand OpenRunCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand DeleteRunCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand MarkPassedCommand { get; }
    public ICommand MarkFailedCommand { get; }
    public ICommand MarkBlockedCommand { get; }
    public ICommand MarkSkippedCommand { get; }
    public ICommand ReportBugCommand { get; }
    public ICommand CompleteRunCommand { get; }

    private void CreateRun()
    {
        var run = new TestRun
        {
            Id = Runs.Count == 0 ? 1 : Runs.Max(r => r.Id) + 1,
            Name = NewRunName.Trim(),
            BuildVersion = string.IsNullOrWhiteSpace(NewRunBuildVersion) ? null : NewRunBuildVersion.Trim(),
            Status = TestRunStatus.InProgress
        };

        Runs.Add(run);
        NewRunName = string.Empty;
        NewRunBuildVersion = null;
        RaiseOverviewChanged();
        OpenRun(run);
    }

    private void OpenRun(TestRun run) => SelectedRun = run;

    private void DeleteRun(TestRun run)
    {
        Runs.Remove(run);
        if (SelectedRun == run) SelectedRun = null;
        RaiseOverviewChanged();
    }

    private void AddItem()
    {
        if (SelectedRun == null) return;

        SelectedRun.Items.Add(new TestRunItem
        {
            Id = SelectedRun.Items.Count == 0 ? 1 : SelectedRun.Items.Max(i => i.Id) + 1,
            TestRunId = SelectedRun.Id,
            CaseName = NewItemCaseName.Trim()
        });

        NewItemCaseName = string.Empty;
    }

    private void SetStatus(object? param, TestRunItemStatus status)
    {
        if (param is TestRunItem item)
        {
            item.Status = status;
            RaiseOverviewChanged();
        }
    }

    private void ReportBug(TestRunItem? item)
    {
        if (item == null) return;

        if (item.Status != TestRunItemStatus.Failed)
        {
            MessageBox.Show("Mark the item as Failed before reporting a bug.", "Report Bug",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (item.LinkedBugId != null)
        {
            MessageBox.Show("A bug is already linked to this item.", "Report Bug",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Placeholder: wire this up to the real Bugs module once its
        // creation API (via IDataService or BugsViewModel) is settled.
        // For now this just marks the item as linked so the UI reflects intent.
        item.LinkedBugId = -1;
        MessageBox.Show($"Bug reporting for \"{item.CaseName}\" is not wired to the Bugs module yet.",
            "Report Bug", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CompleteRun()
    {
        if (SelectedRun == null) return;

        SelectedRun.Status = TestRunStatus.Completed;
        SelectedRun.CompletedAt = DateTime.Now;
        RaiseOverviewChanged();
        CommandManager.InvalidateRequerySuggested();
    }

    private void RaiseOverviewChanged()
    {
        OnPropertyChanged(nameof(TotalRuns));
        OnPropertyChanged(nameof(InProgressRuns));
        OnPropertyChanged(nameof(CompletedRuns));
        OnPropertyChanged(nameof(OverallPassRateLabel));
    }
}
