using System.Windows;
using System.Windows.Input;

namespace QAHub.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    public SettingsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        ResetTestCasesCommand = new RelayCommand(_ => ResetTestCases());
        ResetBugsCommand = new RelayCommand(_ => ResetBugs());
        ResetTestRunsCommand = new RelayCommand(_ => ResetTestRuns());
        ResetRequirementsCommand = new RelayCommand(_ => ResetRequirements());
        ResetProjectsCommand = new RelayCommand(_ => ResetProjects());
        ResetShipmentsCommand = new RelayCommand(_ => ResetShipments());
        ResetCaparsCommand = new RelayCommand(_ => ResetCapars());
        ResetRmasCommand = new RelayCommand(_ => ResetRmas());
        ResetKnowledgeBaseCommand = new RelayCommand(_ => ResetKnowledgeBase());
        ResetAllCommand = new RelayCommand(_ => ResetAll());
    }

    public ICommand ResetTestCasesCommand { get; }
    public ICommand ResetBugsCommand { get; }
    public ICommand ResetTestRunsCommand { get; }
    public ICommand ResetRequirementsCommand { get; }
    public ICommand ResetProjectsCommand { get; }
    public ICommand ResetShipmentsCommand { get; }
    public ICommand ResetCaparsCommand { get; }
    public ICommand ResetRmasCommand { get; }
    public ICommand ResetKnowledgeBaseCommand { get; }
    public ICommand ResetAllCommand { get; }

    private bool Confirm(string message)
    {
        return MessageBox.Show(message, "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            == MessageBoxResult.Yes;
    }

    private void ResetTestCases()
    {
        if (!Confirm("Delete all test cases? This cannot be undone.")) return;
        DeleteAllTestCases();
    }

    private void ResetBugs()
    {
        if (!Confirm("Delete all bugs? This cannot be undone.")) return;
        DeleteAllBugs();
    }

    private void ResetTestRuns()
    {
        if (!Confirm("Delete all test runs? This cannot be undone.")) return;
        DeleteAllTestRuns();
    }

    private void ResetRequirements()
    {
        if (!Confirm("Delete all requirements? This cannot be undone.")) return;
        DeleteAllRequirements();
    }

    private void ResetProjects()
    {
        if (!Confirm("Delete all projects? This cannot be undone.")) return;
        DeleteAllProjects();
    }

    private void ResetShipments()
    {
        if (!Confirm("Delete all shipments? This cannot be undone.")) return;
        DeleteAllShipments();
    }

    private void ResetCapars()
    {
        if (!Confirm("Delete all CAPARs? This cannot be undone.")) return;
        DeleteAllCapars();
    }

    private void ResetRmas()
    {
        if (!Confirm("Delete all RMAs? This cannot be undone.")) return;
        DeleteAllRmas();
    }

    private void ResetKnowledgeBase()
    {
        if (!Confirm("Delete all knowledge base articles? This cannot be undone.")) return;
        DeleteAllKnowledgeArticles();
    }

    private void ResetAll()
    {
        if (!Confirm(
            "Delete ALL data across every section — Test Cases, Bugs, Test Runs, Requirements, " +
            "Projects, Shipments, CAPARs, RMAs, and Knowledge Base? This cannot be undone."))
            return;

        DeleteAllTestCases();
        DeleteAllBugs();
        DeleteAllTestRuns();
        DeleteAllRequirements();
        DeleteAllProjects();
        DeleteAllShipments();
        DeleteAllCapars();
        DeleteAllRmas();
        DeleteAllKnowledgeArticles();
    }

    // TestCases/Bugs' DeleteCommand deletes whatever is currently Selected
    // (it doesn't take the item as a parameter like the newer view models do),
    // so each item must be selected first.
    private void DeleteAllTestCases()
    {
        var items = _mainViewModel.TestCases.Items;
        while (items.Count > 0)
        {
            _mainViewModel.TestCases.Selected = items[0];
            _mainViewModel.TestCases.DeleteCommand.Execute(null);
        }
    }

    private void DeleteAllBugs()
    {
        var items = _mainViewModel.Bugs.Items;
        while (items.Count > 0)
        {
            _mainViewModel.Bugs.Selected = items[0];
            _mainViewModel.Bugs.DeleteCommand.Execute(null);
        }
    }

    // TestRuns/Requirements/Projects each show their own per-item confirmation
    // dialog on delete. Settings already obtained one overarching confirmation
    // above, so bulk resets use each view model's ClearAll() to bypass that
    // per-item dialog rather than looping the confirm-carrying delete command.
    private void DeleteAllTestRuns() => _mainViewModel.TestRuns.ClearAll();
    private void DeleteAllRequirements() => _mainViewModel.Requirements.ClearAll();
    private void DeleteAllProjects() => _mainViewModel.Projects.ClearAll();

    private void DeleteAllShipments()
    {
        var shipments = _mainViewModel.Shipments.Shipments;
        while (shipments.Count > 0)
        {
            _mainViewModel.Shipments.DeleteShipmentCommand.Execute(shipments[0]);
        }
    }

    private void DeleteAllCapars()
    {
        var items = _mainViewModel.Capars.Items;
        while (items.Count > 0)
        {
            _mainViewModel.Capars.DeleteItemCommand.Execute(items[0]);
        }
    }

    private void DeleteAllRmas()
    {
        var items = _mainViewModel.Rmas.Items;
        while (items.Count > 0)
        {
            _mainViewModel.Rmas.DeleteItemCommand.Execute(items[0]);
        }
    }

    private void DeleteAllKnowledgeArticles() => _mainViewModel.KnowledgeBase.ClearAll();
}
