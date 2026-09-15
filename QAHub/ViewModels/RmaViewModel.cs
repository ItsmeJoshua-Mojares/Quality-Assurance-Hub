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
/// Tracks returned units (RMAs) through their disposition flow. Project-based
/// dashboard (mirrors Shipment Tracker): project cards first, click into a
/// project to see its RMAs, read-only detail, and a shared create/edit form.
/// Persists to JSON.
/// </summary>
public class RmaViewModel : ObservableObject
{
    private const string Unassigned = "Unassigned";

    private readonly IDataService _dataService;
    private readonly ProjectsViewModel _projectsVm;

    private string _searchText = string.Empty;
    private string? _selectedProject;
    private Rma? _selected;
    private bool _isEditing;
    private bool _isCreatingNew;

    // Shared draft fields used by both "create new" and "edit existing" forms
    private string _draftRmaNumber = string.Empty;
    private string _draftSerialNumber = string.Empty;
    private string _draftModel = string.Empty;
    private string _draftProject = string.Empty;
    private string _draftCustomer = string.Empty;
    private DateTime _draftReceivedDate = DateTime.Today;
    private string _draftReason = string.Empty;
    private RmaStatus _draftStatus = RmaStatus.Received;
    private string _draftRemarks = string.Empty;

    public RmaViewModel(IDataService dataService, ProjectsViewModel projectsVm)
    {
        _dataService = dataService;
        _projectsVm = projectsVm;

        foreach (var rma in dataService.LoadRmAs())
            Items.Add(rma);

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
        RebuildSummaries();

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftSerialNumber));
        CancelFormCommand = new RelayCommand(_ => GoToProjectItems());
        OpenItemCommand = new RelayCommand(param => { if (param is Rma r) OpenReadOnly(r); });
        EditItemCommand = new RelayCommand(param => { if (param is Rma r) OpenEditForm(r); });
        OpenProjectCommand = new RelayCommand(param => { if (param is ProjectItemSummary s) OpenProject(s); });
        BackToProjectsCommand = new RelayCommand(_ => GoToProjects());
        BackToListCommand = new RelayCommand(_ => GoToProjectItems());
        DeleteItemCommand = new RelayCommand(param => { if (param is Rma r) Delete(r); });
        RefreshCommand = new RelayCommand(_ => Refresh());
    }

    public ObservableCollection<Rma> Items { get; } = new();

    public RmaStatus[] Statuses { get; } = (RmaStatus[])Enum.GetValues(typeof(RmaStatus));

    /// <summary>Filtered view of the project dashboard (each row = project + item count).</summary>
    public ICollectionView ProjectSummariesView { get; private set; } = new ListCollectionView(new List<ProjectItemSummary>());

    /// <summary>Filtered view of the selected project's RMAs, driven by SearchText.</summary>
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

    public Rma? Selected
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

    public string FormTitle => IsCreatingNew ? "New RMA" : "Editing RMA";
    public string SaveButtonLabel => IsCreatingNew ? "Create RMA" : "Save Changes";

    /// <summary>Lock the project to the one being viewed when creating in place.</summary>
    public bool IsProjectLocked => IsCreatingNew && SelectedProject != null && SelectedProject != Unassigned;
    public bool CanChangeProject => !IsProjectLocked;

    // ---- Draft form fields ----
    public string DraftRmaNumber
    {
        get => _draftRmaNumber;
        set => SetProperty(ref _draftRmaNumber, value);
    }

    public string DraftSerialNumber
    {
        get => _draftSerialNumber;
        set { SetProperty(ref _draftSerialNumber, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string DraftModel
    {
        get => _draftModel;
        set => SetProperty(ref _draftModel, value);
    }

    public string DraftProject
    {
        get => _draftProject;
        set => SetProperty(ref _draftProject, value);
    }

    public string DraftCustomer
    {
        get => _draftCustomer;
        set => SetProperty(ref _draftCustomer, value);
    }

    public DateTime DraftReceivedDate
    {
        get => _draftReceivedDate;
        set => SetProperty(ref _draftReceivedDate, value);
    }

    public string DraftReason
    {
        get => _draftReason;
        set => SetProperty(ref _draftReason, value);
    }

    public RmaStatus DraftStatus
    {
        get => _draftStatus;
        set => SetProperty(ref _draftStatus, value);
    }

    public string DraftRemarks
    {
        get => _draftRemarks;
        set => SetProperty(ref _draftRemarks, value);
    }

    // ---- Overview stats ----
    public int TotalItems => Items.Count;
    public int OpenCount => Items.Count(i => i.Status != RmaStatus.Closed && i.Status != RmaStatus.ReShipped);
    public int SubmittedCount => Items.Count(i => i.Status == RmaStatus.Received);
    public int RepairCount => Items.Count(i => i.Status == RmaStatus.Diagnosing || i.Status == RmaStatus.Repairing);
    public int ReturnedCount => Items.Count(i => i.Status == RmaStatus.ReTesting || i.Status == RmaStatus.ReShipped);
    public int ClosedCount => Items.Count(i => i.Status == RmaStatus.Closed);

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

    private static bool ProjectMatches(Rma rma, string project)
    {
        if (project == Unassigned) return string.IsNullOrWhiteSpace(rma.Project);
        return rma.Project.Equals(project, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterItem(object obj)
    {
        if (obj is not Rma rma) return false;

        if (SelectedProject != null && !ProjectMatches(rma, SelectedProject))
            return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return rma.RmaNumber.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || rma.SerialNumber.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || rma.Model.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || rma.Customer.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || rma.Project.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || rma.Reason.Contains(needle, StringComparison.OrdinalIgnoreCase);
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
        DraftRmaNumber = NextNumber();
        DraftProject = SelectedProject == null || SelectedProject == Unassigned ? string.Empty : SelectedProject;
        Selected = null;
        IsEditing = false;
        IsCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void OpenReadOnly(Rma rma)
    {
        Selected = rma;
        IsEditing = false;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void OpenEditForm(Rma rma)
    {
        Selected = rma;
        DraftRmaNumber = rma.RmaNumber;
        DraftSerialNumber = rma.SerialNumber;
        DraftModel = rma.Model;
        DraftProject = rma.Project;
        DraftCustomer = rma.Customer;
        DraftReceivedDate = rma.ReceivedDate;
        DraftReason = rma.Reason;
        DraftStatus = rma.Status;
        DraftRemarks = rma.Remarks ?? string.Empty;
        IsEditing = true;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        Rma? savedItem = null;

        if (IsCreatingNew)
        {
            savedItem = new Rma
            {
                Id = Items.Count == 0 ? 1 : Items.Max(i => i.Id) + 1,
                RmaNumber = string.IsNullOrWhiteSpace(DraftRmaNumber) ? NextNumber() : DraftRmaNumber.Trim(),
                SerialNumber = DraftSerialNumber.Trim(),
                Model = DraftModel.Trim(),
                Project = DraftProject.Trim(),
                Customer = DraftCustomer.Trim(),
                ReceivedDate = DraftReceivedDate,
                Reason = DraftReason.Trim(),
                Status = DraftStatus,
                Remarks = string.IsNullOrWhiteSpace(DraftRemarks) ? null : DraftRemarks.Trim()
            };
            Items.Add(savedItem);
        }
        else if (Selected != null)
        {
            Selected.RmaNumber = string.IsNullOrWhiteSpace(DraftRmaNumber) ? Selected.RmaNumber : DraftRmaNumber.Trim();
            Selected.SerialNumber = DraftSerialNumber.Trim();
            Selected.Model = DraftModel.Trim();
            Selected.Project = DraftProject.Trim();
            Selected.Customer = DraftCustomer.Trim();
            Selected.ReceivedDate = DraftReceivedDate;
            Selected.Reason = DraftReason.Trim();
            Selected.Status = DraftStatus;
            Selected.Remarks = string.IsNullOrWhiteSpace(DraftRemarks) ? null : DraftRemarks.Trim();
            savedItem = Selected;
        }

        _dataService.SaveRmAs(Items);
        RebuildSummaries();
        RaiseOverviewChanged();
        if (savedItem != null) OpenReadOnly(savedItem);
        else GoToProjectItems();
    }

    private void Delete(Rma rma)
    {
        Items.Remove(rma);
        if (Selected == rma) Selected = null;
        _dataService.SaveRmAs(Items);
        RebuildSummaries();
        RaiseOverviewChanged();
        GoToProjectItems();
    }

    public void Refresh()
    {
        var reloaded = _dataService.LoadRmAs();
        var selectedId = Selected?.Id;
        var selectedProject = SelectedProject;

        Items.Clear();
        foreach (var rma in reloaded)
            Items.Add(rma);

        Selected = selectedId != null ? reloaded.FirstOrDefault(r => r.Id == selectedId) : null;
        if (selectedProject != null && reloaded.Any(r => ProjectMatches(r, selectedProject)))
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
        DraftRmaNumber = string.Empty;
        DraftSerialNumber = string.Empty;
        DraftModel = string.Empty;
        DraftProject = string.Empty;
        DraftCustomer = string.Empty;
        DraftReceivedDate = DateTime.Today;
        DraftReason = string.Empty;
        DraftStatus = RmaStatus.Received;
        DraftRemarks = string.Empty;
    }

    private string NextNumber()
    {
        var max = Items.Count == 0 ? 0 : Items.Max(i => i.Id);
        return $"RMA-{max + 1:D4}";
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
        OnPropertyChanged(nameof(SubmittedCount));
        OnPropertyChanged(nameof(RepairCount));
        OnPropertyChanged(nameof(ReturnedCount));
        OnPropertyChanged(nameof(ClosedCount));
        OnPropertyChanged(nameof(SelectedProjectItemCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
        OnPropertyChanged(nameof(HasNoProjectSearchResults));
    }
}