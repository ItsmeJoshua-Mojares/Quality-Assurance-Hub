using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

    public BugsViewModel(IDataService dataService, Action? dataChanged = null)
    {
        _dataService = dataService;
        _dataChanged = dataChanged ?? (() => { });

        Items = new ObservableCollection<Bug>(dataService.LoadBugs());
        _nextId = Items.Count == 0 ? 1 : Items.Max(b => b.Id) + 1;

        Items.CollectionChanged += OnItemsChanged;

        AddCommand = new RelayCommand(Add);
        DeleteCommand = new RelayCommand(Delete, _ => Selected is not null);
    }

    public ObservableCollection<Bug> Items { get; }

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

    public RelayCommand AddCommand { get; }
    public RelayCommand DeleteCommand { get; }

    public void Save()
    {
        _dataService.SaveBugs(Items);
        _dataChanged();
    }

    private void Add(object? _)
    {
        var created = new Bug
        {
            Id = _nextId++,
            Title = "New bug",
            OpenedDate = DateTime.Today
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