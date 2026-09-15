using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QAHub.Models;

public enum TestRunStatus
{
    Planned,
    InProgress,
    Completed
}

public enum TestRunItemStatus
{
    NotRun,
    Passed,
    Failed,
    Blocked,
    Skipped
}

/// <summary>
/// A named batch of test cases executed against a specific build/version.
/// Implements INotifyPropertyChanged directly (rather than depending on the
/// ViewModels-layer ObservableObject) so this model stays usable from any layer.
/// </summary>
public class TestRun : INotifyPropertyChanged
{
    private TestRunStatus _status = TestRunStatus.Planned;
    private DateTime? _completedAt;
    private ObservableCollection<TestRunItem> _items = new();

    public TestRun()
    {
        _items.CollectionChanged += OnItemsCollectionChanged;
    }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BuildVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }

    public TestRunStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public DateTime? CompletedAt
    {
        get => _completedAt;
        set { _completedAt = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Has a real setter (rather than a get-only auto-property) so JSON
    /// deserialization reliably populates it — read-only collection
    /// population support in System.Text.Json isn't consistent enough to
    /// rely on here. The setter re-wires CollectionChanged/PropertyChanged
    /// subscriptions onto whatever instance ends up assigned, so rollup
    /// stats keep working correctly after a JSON load replaces the collection.
    /// </summary>
    public ObservableCollection<TestRunItem> Items
    {
        get => _items;
        set
        {
            _items.CollectionChanged -= OnItemsCollectionChanged;
            foreach (var item in _items)
                item.PropertyChanged -= OnItemPropertyChanged;

            _items = value ?? new ObservableCollection<TestRunItem>();
            _items.CollectionChanged += OnItemsCollectionChanged;
            foreach (var item in _items)
                item.PropertyChanged += OnItemPropertyChanged;

            RaiseRollupsChanged();
        }
    }

    // Roll-ups for the UI (stat cards, progress bar). Computed, not stored,
    // and re-raised whenever an item is added/removed or changes status.
    public int TotalCount => Items.Count;
    public int ExecutedCount => Items.Count(i => i.Status != TestRunItemStatus.NotRun);
    public int PassedCount => Items.Count(i => i.Status == TestRunItemStatus.Passed);
    public int FailedCount => Items.Count(i => i.Status == TestRunItemStatus.Failed);
    public int BlockedCount => Items.Count(i => i.Status == TestRunItemStatus.Blocked);
    public int SkippedCount => Items.Count(i => i.Status == TestRunItemStatus.Skipped);

    public int PassRatePercent => ExecutedCount == 0 ? 0 : (int)Math.Round(PassedCount * 100.0 / ExecutedCount);

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TestRunItem item in e.NewItems)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (TestRunItem item in e.OldItems)
            {
                item.PropertyChanged -= OnItemPropertyChanged;
            }
        }

        RaiseRollupsChanged();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TestRunItem.Status))
        {
            RaiseRollupsChanged();
        }
    }

    private void RaiseRollupsChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ExecutedCount));
        OnPropertyChanged(nameof(PassedCount));
        OnPropertyChanged(nameof(FailedCount));
        OnPropertyChanged(nameof(BlockedCount));
        OnPropertyChanged(nameof(SkippedCount));
        OnPropertyChanged(nameof(PassRatePercent));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// A single test case's execution result within a specific run.
/// One TestCase can appear in many runs, each with its own result.
/// </summary>
public class TestRunItem : INotifyPropertyChanged
{
    private TestRunItemStatus _status = TestRunItemStatus.NotRun;
    private string? _notes;
    private int? _linkedBugId;

    public int Id { get; set; }
    public int TestRunId { get; set; }

    /// <summary>
    /// Points at the real TestCase once TestCases has a stable data source
    /// to link against. Nullable until that wiring exists.
    /// </summary>
    public int? TestCaseId { get; set; }

    /// <summary>
    /// Snapshot of the case name shown in the UI. Temporary stand-in until
    /// items are populated from the real TestCases list/data service —
    /// replace with a lookup against TestCaseId once that API is settled.
    /// </summary>
    public string CaseName { get; set; } = string.Empty;

    public TestRunItemStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            ExecutedAt = value == TestRunItemStatus.NotRun ? null : DateTime.Now;
            OnPropertyChanged();
        }
    }

    public string? Notes
    {
        get => _notes;
        set { _notes = value; OnPropertyChanged(); }
    }

    public string? ExecutedBy { get; set; }

    [JsonInclude]
    public DateTime? ExecutedAt { get; private set; }

    /// <summary>
    /// Set when a Failed result is escalated to a bug via "Report Bug".
    /// Null until that happens.
    /// </summary>
    public int? LinkedBugId
    {
        get => _linkedBugId;
        set { _linkedBugId = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}