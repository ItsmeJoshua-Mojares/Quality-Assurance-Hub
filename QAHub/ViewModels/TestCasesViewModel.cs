using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public TestCasesViewModel(IDataService dataService, Action? dataChanged = null)
    {
        _dataService = dataService;
        _dataChanged = dataChanged ?? (() => { });

        Items = new ObservableCollection<TestCase>(dataService.LoadTestCases());
        _nextId = Items.Count == 0 ? 1 : Items.Max(tc => tc.Id) + 1;

        Items.CollectionChanged += OnItemsChanged;

        AddCommand = new RelayCommand(Add);
        DeleteCommand = new RelayCommand(Delete, _ => Selected is not null);
    }

    public ObservableCollection<TestCase> Items { get; }

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

    public RelayCommand AddCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public void Save()
    {
        _dataService.SaveTestCases(Items);
        _dataChanged();
    }

    private void Add(object? _)
    {
        var created = new TestCase
        {
            Id = _nextId++,
            Title = "New test case",
            Status = TestStatus.Draft
        };

        Items.Add(created);
        Selected = created;
        Save();
    }

    private void Delete(object? _)
    {
        if (Selected is null) return;

        Items.Remove(Selected);
        Selected = null;
        Save();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => Save();

    private static void CommandManagerInvalidate()
        => Dispatcher.CurrentDispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
}