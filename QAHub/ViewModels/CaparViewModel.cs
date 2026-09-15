using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using QAHub.Models;
using QAHub.Services;

namespace QAHub.ViewModels;

/// <summary>
/// Tracks corrective &amp; preventive action requests (CAPARs). Project-based
/// dashboard (mirrors Shipment Tracker): project cards first, click into a
/// project to see its CAPARs, read-only detail, and a shared create/edit form.
/// Persists to JSON.
/// </summary>
public class CaparViewModel : ObservableObject
{
    private const string Unassigned = "Unassigned";

    private readonly IDataService _dataService;
    private readonly ProjectsViewModel _projectsVm;

    private string _searchText = string.Empty;
    private string? _selectedProject;
    private Capar? _selected;
    private bool _isEditing;
    private bool _isCreatingNew;

    // Shared draft fields used by both "create new" and "edit existing" forms
    private string _draftCaparNumber = string.Empty;
    private string _draftTitle = string.Empty;
    private CaparSource _draftSource = CaparSource.Internal;
    private string _draftProject = string.Empty;
    private Severity _draftSeverity = Severity.Normal;
    private string _draftRootCause = string.Empty;
    private string _draftCorrectiveAction = string.Empty;
    private string _draftPreventiveAction = string.Empty;
    private string _draftOwner = string.Empty;
    private CaparStatus _draftStatus = CaparStatus.Open;
    private DateTime _draftOpenedDate = DateTime.Today;
    private DateTime? _draftDueDate;
    private DateTime? _draftClosedDate;
    private string _draftRemarks = string.Empty;

    public CaparViewModel(IDataService dataService, ProjectsViewModel projectsVm)
    {
        _dataService = dataService;
        _projectsVm = projectsVm;

        foreach (var capar in dataService.LoadCapars())
            Items.Add(capar);

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
        RebuildSummaries();

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftTitle));
        CancelFormCommand = new RelayCommand(_ => GoToProjectItems());
        OpenItemCommand = new RelayCommand(param => { if (param is Capar c) OpenReadOnly(c); });
        EditItemCommand = new RelayCommand(param => { if (param is Capar c) OpenEditForm(c); });
        OpenProjectCommand = new RelayCommand(param => { if (param is ProjectItemSummary s) OpenProject(s); });
        BackToProjectsCommand = new RelayCommand(_ => GoToProjects());
        BackToListCommand = new RelayCommand(_ => GoToProjectItems());
        DeleteItemCommand = new RelayCommand(param => { if (param is Capar c) Delete(c); });
        RefreshCommand = new RelayCommand(_ => Refresh());
    }

    public ObservableCollection<Capar> Items { get; } = new();

    public CaparSource[] Sources { get; } = (CaparSource[])Enum.GetValues(typeof(CaparSource));
    public Severity[] Severities { get; } = (Severity[])Enum.GetValues(typeof(Severity));
    public CaparStatus[] Statuses { get; } = (CaparStatus[])Enum.GetValues(typeof(CaparStatus));

    /// <summary>Filtered view of the project dashboard (each row = project + item count).</summary>
    public ICollectionView ProjectSummariesView { get; private set; } = new ListCollectionView(new List<ProjectItemSummary>());

    /// <summary>Filtered view of the selected project's CAPARs, driven by SearchText.</summary>
    public ICollectionView ItemsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ItemsView.Refresh();
                ProjectSummariesView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(SelectedProjectItemCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
                OnPropertyChanged(nameof(HasNoProjectSearchResults));
            }
        }
    }

    public string? SelectedProject
    {
        get => _selectedProject;
        private set => SetProperty(ref _selectedProject, value);
    }

    public string? SelectedProjectDisplayName => SelectedProject;

    public Capar? Selected
    {
        get => _selected;
        private set => SetProperty(ref _selected, value);
    }

    private bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    private bool IsCreatingNew
    {
        get => _isCreatingNew;
        set => SetProperty(ref _isCreatingNew, value);
    }

    public bool IsProjectsView => SelectedProject == null && Selected == null && !IsCreatingNew && !IsEditing;
    public bool IsProjectItemsView => SelectedProject != null && Selected == null && !IsCreatingNew && !IsEditing;
    public bool IsReadOnlyView => Selected != null && !IsEditing && !IsCreatingNew;
    public bool IsFormView => IsCreatingNew || (Selected != null && IsEditing);

    public string FormTitle => IsCreatingNew ? "New CAPAR" : "Editing CAPAR";
    public string SaveButtonLabel => IsCreatingNew ? "Create CAPAR" : "Save Changes";

    /// <summary>Lock the project to the one being viewed when creating in place.</summary>
    public bool IsProjectLocked => IsCreatingNew && SelectedProject != null && SelectedProject != Unassigned;
    public bool CanChangeProject => !IsProjectLocked;

    // ---- Draft form fields ----
    public string DraftCaparNumber
    {
        get => _draftCaparNumber;
        set => SetProperty(ref _draftCaparNumber, value);
    }

    public string DraftTitle
    {
        get => _draftTitle;
        set { SetProperty(ref _draftTitle, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public CaparSource DraftSource
    {
        get => _draftSource;
        set => SetProperty(ref _draftSource, value);
    }

    public string DraftProject
    {
        get => _draftProject;
        set => SetProperty(ref _draftProject, value);
    }

    public Severity DraftSeverity
    {
        get => _draftSeverity;
        set => SetProperty(ref _draftSeverity, value);
    }

    public string DraftRootCause
    {
        get => _draftRootCause;
        set => SetProperty(ref _draftRootCause, value);
    }

    public string DraftCorrectiveAction
    {
        get => _draftCorrectiveAction;
        set => SetProperty(ref _draftCorrectiveAction, value);
    }

    public string DraftPreventiveAction
    {
        get => _draftPreventiveAction;
        set => SetProperty(ref _draftPreventiveAction, value);
    }

    public string DraftOwner
    {
        get => _draftOwner;
        set => SetProperty(ref _draftOwner, value);
    }

    public CaparStatus DraftStatus
    {
        get => _draftStatus;
        set => SetProperty(ref _draftStatus, value);
    }

    public DateTime DraftOpenedDate
    {
        get => _draftOpenedDate;
        set => SetProperty(ref _draftOpenedDate, value);
    }

    public DateTime? DraftDueDate
    {
        get => _draftDueDate;
        set => SetProperty(ref _draftDueDate, value);
    }

    public DateTime? DraftClosedDate
    {
        get => _draftClosedDate;
        set => SetProperty(ref _draftClosedDate, value);
    }

    public string DraftRemarks
    {
        get => _draftRemarks;
        set => SetProperty(ref _draftRemarks, value);
    }

    // ---- Overview stats ----
    public int TotalItems => Items.Count;
    public int OpenCount => Items.Count(i => i.Status == CaparStatus.Open || i.Status == CaparStatus.InProgress);
    public int ImplementedCount => Items.Count(i => i.Status == CaparStatus.Implemented);
    public int VerifiedCount => Items.Count(i => i.Status == CaparStatus.Verified);
    public int ClosedCount => Items.Count(i => i.Status == CaparStatus.Closed);
    public int CriticalCount => Items.Count(i => i.Severity == Severity.Critical);

    public int SelectedProjectItemCount =>
        SelectedProject == null ? 0 : Items.Count(i => ProjectMatches(i, SelectedProject));

    public int FilteredCount => ItemsView.Cast<object>().Count();
    public bool HasNoSearchResults => SelectedProjectItemCount > 0 && FilteredCount == 0;
    public bool HasNoProjectSearchResults => TotalItems > 0 && ProjectSummariesView.Cast<object>().Count() == 0;

    public ICommand ShowCreateFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand OpenItemCommand { get; }
    public ICommand EditItemCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand BackToProjectsCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand DeleteItemCommand { get; }
    public ICommand RefreshCommand { get; }

    /// <summary>Live project names for the editable form picker, refreshed on nav.</summary>
    public ObservableCollection<string> Projects { get; } = new();

    public void RefreshProjects(IEnumerable<string> projectNames)
    {
        Projects.Clear();
        foreach (var name in projectNames)
            Projects.Add(name);
        OnPropertyChanged(nameof(Projects));
    }

    private static bool ProjectMatches(Capar capar, string project)
    {
        if (project == Unassigned) return string.IsNullOrWhiteSpace(capar.Project);
        return capar.Project.Equals(project, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterItem(object obj)
    {
        if (obj is not Capar capar) return false;

        if (SelectedProject != null && !ProjectMatches(capar, SelectedProject))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return capar.CaparNumber.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || capar.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || capar.Project.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || capar.Owner.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || capar.RootCause.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || capar.Source.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterProjectSummary(object obj)
    {
        if (obj is not ProjectItemSummary summary) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        return summary.ProjectName.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void RebuildSummaries()
    {
        var unassignedCount = Items.Count(i => string.IsNullOrWhiteSpace(i.Project));

        var summaries = _projectsVm.Projects
            .Select(p => new ProjectItemSummary
            {
                Project = p,
                ItemCount = Items.Count(i => ProjectMatches(i, p.Name))
            })
            .ToList();

        if (unassignedCount > 0)
            summaries.Add(new ProjectItemSummary { Project = null, ItemCount = unassignedCount });

        ProjectSummariesView = new ListCollectionView(summaries);
        ProjectSummariesView.Filter = FilterProjectSummary;
        OnPropertyChanged(nameof(ProjectSummariesView));
        OnPropertyChanged(nameof(HasNoProjectSearchResults));
    }

    private void OpenProject(ProjectItemSummary summary)
    {
        SelectedProject = summary.ProjectName;
        Selected = null;
        IsEditing = false;
        IsCreatingNew = false;
        SearchText = string.Empty;
        ItemsView.Refresh();
        RaiseViewStateChanged();
    }

    private void ShowCreateForm()
    {
        ClearDraft();
        DraftCaparNumber = NextNumber();
        DraftProject = SelectedProject == null || SelectedProject == Unassigned ? string.Empty : SelectedProject;
        Selected = null;
        IsEditing = false;
        IsCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void OpenReadOnly(Capar capar)
    {
        Selected = capar;
        IsEditing = false;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void OpenEditForm(Capar capar)
    {
        Selected = capar;
        DraftCaparNumber = capar.CaparNumber;
        DraftTitle = capar.Title;
        DraftSource = capar.Source;
        DraftProject = capar.Project;
        DraftSeverity = capar.Severity;
        DraftRootCause = capar.RootCause;
        DraftCorrectiveAction = capar.CorrectiveAction;
        DraftPreventiveAction = capar.PreventiveAction;
        DraftOwner = capar.Owner;
        DraftStatus = capar.Status;
        DraftOpenedDate = capar.OpenedDate;
        DraftDueDate = capar.DueDate;
        DraftClosedDate = capar.ClosedDate;
        DraftRemarks = capar.Remarks ?? string.Empty;
        IsEditing = true;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        Capar? savedItem = null;

        if (IsCreatingNew)
        {
            savedItem = new Capar
            {
                Id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1,
                CaparNumber = string.IsNullOrWhiteSpace(DraftCaparNumber) ? NextNumber() : DraftCaparNumber.Trim(),
                Title = DraftTitle.Trim(),
                Source = DraftSource,
                Project = DraftProject.Trim(),
                Severity = DraftSeverity,
                RootCause = DraftRootCause.Trim(),
                CorrectiveAction = DraftCorrectiveAction.Trim(),
                PreventiveAction = DraftPreventiveAction.Trim(),
                Owner = DraftOwner.Trim(),
                Status = DraftStatus,
                OpenedDate = DraftOpenedDate,
                DueDate = DraftDueDate,
                ClosedDate = DraftClosedDate,
                Remarks = string.IsNullOrWhiteSpace(DraftRemarks) ? null : DraftRemarks.Trim()
            };
            Items.Add(savedItem);
        }
        else if (Selected != null)
        {
            Selected.CaparNumber = string.IsNullOrWhiteSpace(DraftCaparNumber) ? Selected.CaparNumber : DraftCaparNumber.Trim();
            Selected.Title = DraftTitle.Trim();
            Selected.Source = DraftSource;
            Selected.Project = DraftProject.Trim();
            Selected.Severity = DraftSeverity;
            Selected.RootCause = DraftRootCause.Trim();
            Selected.CorrectiveAction = DraftCorrectiveAction.Trim();
            Selected.PreventiveAction = DraftPreventiveAction.Trim();
            Selected.Owner = DraftOwner.Trim();
            Selected.Status = DraftStatus;
            Selected.OpenedDate = DraftOpenedDate;
            Selected.DueDate = DraftDueDate;
            Selected.ClosedDate = DraftClosedDate;
            Selected.Remarks = string.IsNullOrWhiteSpace(DraftRemarks) ? null : DraftRemarks.Trim();
            savedItem = Selected;
        }

        _dataService.SaveCapars(Items);
        RebuildSummaries();
        RaiseOverviewChanged();
        if (savedItem != null) OpenReadOnly(savedItem);
        else GoToProjectItems();
    }

    private void Delete(Capar capar)
    {
        Items.Remove(capar);
        if (Selected == capar) Selected = null;
        _dataService.SaveCapars(Items);
        RebuildSummaries();
        RaiseOverviewChanged();
        GoToProjectItems();
    }

    public void Refresh()
    {
        var reloaded = _dataService.LoadCapars();
        var selectedId = Selected?.Id;
        var selectedProject = SelectedProject;

        Items.Clear();
        foreach (var capar in reloaded)
            Items.Add(capar);

        Selected = selectedId != null ? reloaded.FirstOrDefault(c => c.Id == selectedId) : null;
        if (selectedProject != null && reloaded.Any(c => ProjectMatches(c, selectedProject)))
            SelectedProject = selectedProject;

        RebuildSummaries();
        ItemsView.Refresh();
        RaiseOverviewChanged();
        RaiseViewStateChanged();
    }

    private void GoToProjects()
    {
        SelectedProject = null;
        Selected = null;
        IsEditing = false;
        IsCreatingNew = false;
        SearchText = string.Empty;
        RebuildSummaries();
        RaiseViewStateChanged();
    }

    private void GoToProjectItems()
    {
        Selected = null;
        IsEditing = false;
        IsCreatingNew = false;
        ItemsView.Refresh();
        RaiseViewStateChanged();
    }

    private void ClearDraft()
    {
        DraftCaparNumber = string.Empty;
        DraftTitle = string.Empty;
        DraftSource = CaparSource.Internal;
        DraftProject = string.Empty;
        DraftSeverity = Severity.Normal;
        DraftRootCause = string.Empty;
        DraftCorrectiveAction = string.Empty;
        DraftPreventiveAction = string.Empty;
        DraftOwner = string.Empty;
        DraftStatus = CaparStatus.Open;
        DraftOpenedDate = DateTime.Today;
        DraftDueDate = null;
        DraftClosedDate = null;
        DraftRemarks = string.Empty;
    }

    private string NextNumber()
    {
        var max = Items.Count == 0 ? 0 : Items.Max(i => i.Id);
        return $"CAPAR-{max + 1:D4}";
    }

    private void RaiseViewStateChanged()
    {
        OnPropertyChanged(nameof(IsProjectsView));
        OnPropertyChanged(nameof(IsProjectItemsView));
        OnPropertyChanged(nameof(IsReadOnlyView));
        OnPropertyChanged(nameof(IsFormView));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonLabel));
        OnPropertyChanged(nameof(SelectedProjectDisplayName));
        OnPropertyChanged(nameof(SelectedProjectItemCount));
        OnPropertyChanged(nameof(IsProjectLocked));
        OnPropertyChanged(nameof(CanChangeProject));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RaiseOverviewChanged()
    {
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(OpenCount));
        OnPropertyChanged(nameof(ImplementedCount));
        OnPropertyChanged(nameof(VerifiedCount));
        OnPropertyChanged(nameof(ClosedCount));
        OnPropertyChanged(nameof(CriticalCount));
        OnPropertyChanged(nameof(SelectedProjectItemCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
        OnPropertyChanged(nameof(HasNoProjectSearchResults));
    }
}