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

public class TestCasesViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly Action _dataChanged;
    private int _nextId;
    private TestCase? _selected;
    private string _searchText = string.Empty;
    private bool _isCreatingNew;

    // Draft fields for the inline "Add Test Case" form
    private string _draftTitle = string.Empty;
    private string _draftDescription = string.Empty;
    private string _draftArea = string.Empty;
    private Priority _draftPriority;
    private TestStatus _draftStatus;

    public TestCasesViewModel(IDataService dataService, Action? dataChanged = null)
    {
        _dataService = dataService;
        _dataChanged = dataChanged ?? (() => { });

        Items = new ObservableCollection<TestCase>(dataService.LoadTestCases());
        _nextId = Items.Count == 0 ? 1 : Items.Max(tc => tc.Id) + 1;

        Items.CollectionChanged += OnItemsChanged;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        DeleteCommand = new RelayCommand(Delete, _ => Selected is not null);

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftTitle));
        CancelFormCommand = new RelayCommand(_ => GoToList());
    }

    public ObservableCollection<TestCase> Items { get; }

    /// <summary>Filtered view of Items for the grid, driven by SearchText (matches title or area).</summary>
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

    public TestCase? Selected
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

    public IReadOnlyList<Priority> Priorities => Enum.GetValues<Priority>();
    public IReadOnlyList<TestStatus> Statuses => Enum.GetValues<TestStatus>();

    /// <summary>Front page: the search bar + grid.</summary>
    public bool IsListView => !_isCreatingNew;

    /// <summary>Inline "Add Test Case" form, shown instead of the grid.</summary>
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

    public string DraftArea
    {
        get => _draftArea;
        set => SetProperty(ref _draftArea, value);
    }

    public Priority DraftPriority
    {
        get => _draftPriority;
        set => SetProperty(ref _draftPriority, value);
    }

    public TestStatus DraftStatus
    {
        get => _draftStatus;
        set => SetProperty(ref _draftStatus, value);
    }

    public RelayCommand DeleteCommand { get; }
    public RelayCommand ShowCreateFormCommand { get; }
    public RelayCommand SaveFormCommand { get; }
    public RelayCommand CancelFormCommand { get; }

    public void Save()
    {
        _dataService.SaveTestCases(Items);
        _dataChanged();
    }

    private bool FilterItem(object obj)
    {
        if (obj is not TestCase testCase) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return testCase.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (testCase.Area?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ShowCreateForm()
    {
        DraftTitle = string.Empty;
        DraftDescription = string.Empty;
        DraftArea = string.Empty;
        DraftPriority = Priorities.Count > 0 ? Priorities[0] : default;
        DraftStatus = TestStatus.Draft;

        _isCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        var created = new TestCase
        {
            Id = _nextId++,
            Title = DraftTitle.Trim(),
            Description = DraftDescription.Trim(),
            Area = DraftArea.Trim(),
            Priority = DraftPriority,
            Status = DraftStatus
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
                $"Delete test case \"{Selected.Title}\"? This cannot be undone.",
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