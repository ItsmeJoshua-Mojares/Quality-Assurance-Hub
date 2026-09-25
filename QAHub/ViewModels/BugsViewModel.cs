using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using QAHub.Models;
using QAHub.Services;

namespace QAHub.ViewModels;

public class BugsViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly Action _dataChanged;
    private int _nextId;
    private Bug? _selected;
    private string _searchText = string.Empty;
    private bool _isCreatingNew;

    // Draft fields for the inline "Log Defect" form
    private string _draftTitle = string.Empty;
    private string _draftDescription = string.Empty;
    private Severity _draftSeverity;
    private BugStatus _draftStatus;
    private string _draftAssignee = string.Empty;
    private DateTime _draftOpenedDate = DateTime.Today;

    public BugsViewModel(IDataService dataService, Action? dataChanged = null)
    {
        _dataService = dataService;
        _dataChanged = dataChanged ?? (() => { });

        Items = new ObservableCollection<Bug>(dataService.LoadBugs());
        _nextId = Items.Count == 0 ? 1 : Items.Max(b => b.Id) + 1;

        Items.CollectionChanged += OnItemsChanged;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        DeleteCommand = new RelayCommand(Delete, _ => Selected is not null);

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftTitle));
        CancelFormCommand = new RelayCommand(_ => GoToList());
    }

    public ObservableCollection<Bug> Items { get; }

    /// <summary>Filtered view of Items for the grid, driven by SearchText (matches title or assignee).</summary>
    public ICollectionView ItemsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ItemsView.Refresh();
            }
        }
    }

    public Bug? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                CommandManagerInvalidate();
            }
        }
    }

    public IReadOnlyList<Severity> Severities => Enum.GetValues<Severity>();
    public IReadOnlyList<BugStatus> Statuses => Enum.GetValues<BugStatus>();

    /// <summary>Front page: the search bar + grid.</summary>
    public bool IsListView => !_isCreatingNew;

    /// <summary>Inline "Log Defect" form, shown instead of the grid.</summary>
    public bool IsFormView => _isCreatingNew;

    public string DraftTitle
    {
        get => _draftTitle;
        set { SetProperty(ref _draftTitle, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set => SetProperty(ref _draftDescription, value);
    }

    public Severity DraftSeverity
    {
        get => _draftSeverity;
        set => SetProperty(ref _draftSeverity, value);
    }

    public BugStatus DraftStatus
    {
        get => _draftStatus;
        set => SetProperty(ref _draftStatus, value);
    }

    public string DraftAssignee
    {
        get => _draftAssignee;
        set => SetProperty(ref _draftAssignee, value);
    }

    public DateTime DraftOpenedDate
    {
        get => _draftOpenedDate;
        set => SetProperty(ref _draftOpenedDate, value);
    }

    public RelayCommand DeleteCommand { get; }
    public RelayCommand ShowCreateFormCommand { get; }
    public RelayCommand SaveFormCommand { get; }
    public RelayCommand CancelFormCommand { get; }

    public void Save()
    {
        _dataService.SaveBugs(Items);
        _dataChanged();
    }

    private bool FilterItem(object obj)
    {
        if (obj is not Bug bug) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return bug.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (bug.Assignee?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ShowCreateForm()
    {
        DraftTitle = string.Empty;
        DraftDescription = string.Empty;
        DraftSeverity = Severities.Count > 0 ? Severities[0] : default;
        DraftStatus = Statuses.Count > 0 ? Statuses[0] : default;
        DraftAssignee = string.Empty;
        DraftOpenedDate = DateTime.Today;

        _isCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        var created = new Bug
        {
            Id = _nextId++,
            Title = DraftTitle.Trim(),
            Description = DraftDescription.Trim(),
            Severity = DraftSeverity,
            Status = DraftStatus,
            Assignee = DraftAssignee.Trim(),
            OpenedDate = DraftOpenedDate
        };

        Items.Add(created);
        Selected = created;
        Save();
        GoToList();
    }

    private void GoToList()
    {
        _isCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void RaiseViewStateChanged()
    {
        OnPropertyChanged(nameof(IsListView));
        OnPropertyChanged(nameof(IsFormView));
        CommandManager.InvalidateRequerySuggested();
    }

    private void Delete(object? _)
    {
        if (Selected is null) return;

        if (MessageBox.Show(
                $"Delete bug \"{Selected.Title}\"? This cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        Items.Remove(Selected);
        Selected = null;
        Save();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Save();

    private static void CommandManagerInvalidate()
        => Dispatcher.CurrentDispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());

    public void ClearAll()
    {
        Items.Clear();
        Selected = null;
        Save();
    }
}