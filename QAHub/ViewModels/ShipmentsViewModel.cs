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
/// Tracks every physical shipment that goes out, each tied to a Project (via
/// ProjectId). Each shipment records the logistics leg (shipment date, serial
/// number, model, firmware, location) plus the QA gate on the firmware build
/// (QA status). Flow: project dashboard (list of projects) -> project's
/// shipped serials -> read-only detail -> create/edit form. Persists to JSON.
/// </summary>
public class ShipmentsViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ProjectsViewModel _projects;

    private string _searchText = string.Empty;

    private Project? _selectedProject;
    private Shipment? _selectedShipment;
    private bool _isEditing;
    private bool _isCreatingNew;

    // Shared draft fields used by both "create new" and "edit existing" forms
    private DateTime _draftShipmentDate = DateTime.Today;
    private string _draftSerialNumber = string.Empty;
    private string _draftModel = string.Empty;
    private string _draftFirmwareVersion = string.Empty;
    private string _draftLocation = string.Empty;
    private string _draftRemarks = string.Empty;
    private Project? _draftProject;
    private ShipmentQaStatus _draftQaStatus = ShipmentQaStatus.Pending;

    public ShipmentsViewModel(IDataService dataService, ProjectsViewModel projects)
    {
        _dataService = dataService;
        _projects = projects;

        foreach (var shipment in dataService.LoadShipments())
            Shipments.Add(shipment);

        ShipmentsView = CollectionViewSource.GetDefaultView(Shipments);
        ShipmentsView.Filter = FilterShipment;
        RebuildProjectSummaries();

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftSerialNumber));
        CancelFormCommand = new RelayCommand(_ => GoToProjectShipments());

        OpenProjectCommand = new RelayCommand(param => { if (param is Project p) OpenProject(p); });
        OpenShipmentCommand = new RelayCommand(param => { if (param is Shipment s) OpenReadOnly(s); });
        EditShipmentCommand = new RelayCommand(param => { if (param is Shipment s) OpenEditForm(s); });
        BackToProjectsCommand = new RelayCommand(_ => GoToProjects());
        BackToListCommand = new RelayCommand(_ => GoToProjectShipments());
        DeleteShipmentCommand = new RelayCommand(param => { if (param is Shipment s) DeleteShipment(s); });
    }

    public ObservableCollection<Shipment> Shipments { get; } = new();

    public Project[] Projects => _projects.Projects.ToArray();

    public ShipmentQaStatus[] QaStatuses { get; } = (ShipmentQaStatus[])Enum.GetValues(typeof(ShipmentQaStatus));

    /// <summary>Filtered view of the project dashboard (each row = project + shipment count).</summary>
    public ICollectionView ProjectSummariesView { get; private set; } = new ListCollectionView(new List<ProjectShipmentSummary>());

    /// <summary>Filtered view of the selected project's shipments, driven by SearchText
    /// (matches serial number, model, firmware, location, or project name).</summary>
    public ICollectionView ShipmentsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ShipmentsView.Refresh();
                ProjectSummariesView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
                OnPropertyChanged(nameof(HasNoProjectSearchResults));
            }
        }
    }

    public Project? SelectedProject
    {
        get => _selectedProject;
        private set => SetProperty(ref _selectedProject, value);
    }

    public Shipment? SelectedShipment
    {
        get => _selectedShipment;
        private set => SetProperty(ref _selectedShipment, value);
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

    public bool IsProjectsView => SelectedProject == null && SelectedShipment == null && !IsCreatingNew && !IsEditing;
    public bool IsProjectShipmentsView => SelectedProject != null && SelectedShipment == null && !IsCreatingNew && !IsEditing;
    public bool IsReadOnlyView => SelectedShipment != null && !IsEditing && !IsCreatingNew;
    public bool IsFormView =>
        IsCreatingNew || (SelectedShipment != null && IsEditing);

    public string FormTitle =>
        IsCreatingNew ? "New Shipment" : "Editing Shipment";

    public string SaveButtonLabel =>
        IsCreatingNew ? "Create Shipment" : "Save Changes";

    // ---- Draft form fields ----
    public Project? DraftProject
    {
        get => _draftProject;
        set => SetProperty(ref _draftProject, value);
    }

    public DateTime DraftShipmentDate
    {
        get => _draftShipmentDate;
        set => SetProperty(ref _draftShipmentDate, value);
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

    public string DraftFirmwareVersion
    {
        get => _draftFirmwareVersion;
        set => SetProperty(ref _draftFirmwareVersion, value);
    }

    public ShipmentQaStatus DraftQaStatus
    {
        get => _draftQaStatus;
        set => SetProperty(ref _draftQaStatus, value);
    }

    public string DraftLocation
    {
        get => _draftLocation;
        set => SetProperty(ref _draftLocation, value);
    }

    public string DraftRemarks
    {
        get => _draftRemarks;
        set => SetProperty(ref _draftRemarks, value);
    }

    // ---- Overview stats ----
    public int TotalProjects => _projects.Projects.Count;
    public int SelectedProjectShipmentCount =>
        SelectedProject == null ? 0 : Shipments.Count(s => s.ProjectId == SelectedProject.Id);

    public int TotalShipments => Shipments.Count;
    public int QaPassedCount => Shipments.Count(s => s.QaStatus == ShipmentQaStatus.Passed);
    public int QaReadyCount => Shipments.Count(s => s.QaStatus == ShipmentQaStatus.Ready);
    public int QaBlockedCount => Shipments.Count(s => s.QaStatus == ShipmentQaStatus.Blocked);
    public int QaInProgressCount => Shipments.Count(s => s.QaStatus == ShipmentQaStatus.InProgress);

    public int FilteredCount => ShipmentsView.Cast<object>().Count();
    public bool HasNoSearchResults => SelectedProjectShipmentCount > 0 && FilteredCount == 0;
    public bool HasNoProjectSearchResults => TotalProjects > 0 && ProjectSummariesView.Cast<object>().Count() == 0;

    public ICommand ShowCreateFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand OpenShipmentCommand { get; }
    public ICommand EditShipmentCommand { get; }
    public ICommand BackToProjectsCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand DeleteShipmentCommand { get; }

    private bool FilterShipment(object obj)
    {
        if (obj is not Shipment shipment) return false;

        if (SelectedProject != null && shipment.ProjectId != SelectedProject.Id)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return shipment.SerialNumber.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || shipment.Model.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || shipment.FirmwareVersion.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (shipment.Location?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false)
            || shipment.ProjectName.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private bool FilterProjectSummary(object obj)
    {
        if (obj is not ProjectShipmentSummary summary) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        return summary.Project.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private void RebuildProjectSummaries()
    {
        var summaries = _projects.Projects
            .Select(p => new ProjectShipmentSummary
            {
                Project = p,
                ShipmentCount = Shipments.Count(s => s.ProjectId == p.Id)
            })
            .ToList();

        ProjectSummariesView = new ListCollectionView(summaries);
        ProjectSummariesView.Filter = FilterProjectSummary;
        OnPropertyChanged(nameof(ProjectSummariesView));
        OnPropertyChanged(nameof(HasNoProjectSearchResults));
    }

    private void OpenProject(Project project)
    {
        SelectedProject = project;
        SelectedShipment = null;
        IsEditing = false;
        IsCreatingNew = false;
        ShipmentsView.Refresh();
        RaiseViewStateChanged();
    }

    private void ShowCreateForm()
    {
        ClearDraft();
        DraftProject = SelectedProject;
        SelectedShipment = null;
        IsEditing = false;
        IsCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void OpenReadOnly(Shipment shipment)
    {
        SelectedShipment = shipment;
        if (SelectedProject == null)
            SelectedProject = _projects.Projects.FirstOrDefault(p => p.Id == shipment.ProjectId);
        IsEditing = false;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void OpenEditForm(Shipment shipment)
    {
        SelectedShipment = shipment;
        DraftProject = SelectedProject ?? _projects.Projects.FirstOrDefault(p => p.Id == shipment.ProjectId);
        DraftShipmentDate = shipment.ShipmentDate;
        DraftSerialNumber = shipment.SerialNumber;
        DraftModel = shipment.Model;
        DraftFirmwareVersion = shipment.FirmwareVersion;
        DraftQaStatus = shipment.QaStatus;
        DraftLocation = shipment.Location ?? string.Empty;
        DraftRemarks = shipment.Remarks ?? string.Empty;
        IsEditing = true;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        if (IsCreatingNew)
        {
            var shipment = new Shipment
            {
                Id = Shipments.Count == 0 ? 1 : Shipments.Max(s => s.Id) + 1,
                ProjectId = DraftProject?.Id ?? 0,
                ProjectName = DraftProject?.Name ?? "Unassigned",
                ShipmentDate = DraftShipmentDate,
                SerialNumber = DraftSerialNumber.Trim(),
                Model = DraftModel.Trim(),
                FirmwareVersion = DraftFirmwareVersion.Trim(),
                QaStatus = DraftQaStatus,
                Location = string.IsNullOrWhiteSpace(DraftLocation) ? null : DraftLocation.Trim(),
                Remarks = string.IsNullOrWhiteSpace(DraftRemarks) ? null : DraftRemarks.Trim()
            };
            Shipments.Add(shipment);
        }
        else if (SelectedShipment != null)
        {
            SelectedShipment.ProjectId = DraftProject?.Id ?? 0;
            SelectedShipment.ProjectName = DraftProject?.Name ?? "Unassigned";
            SelectedShipment.ShipmentDate = DraftShipmentDate;
            SelectedShipment.SerialNumber = DraftSerialNumber.Trim();
            SelectedShipment.Model = DraftModel.Trim();
            SelectedShipment.FirmwareVersion = DraftFirmwareVersion.Trim();
            SelectedShipment.QaStatus = DraftQaStatus;
            SelectedShipment.Location = string.IsNullOrWhiteSpace(DraftLocation) ? null : DraftLocation.Trim();
            SelectedShipment.Remarks = string.IsNullOrWhiteSpace(DraftRemarks) ? null : DraftRemarks.Trim();
        }

        _dataService.SaveShipments(Shipments);
        RaiseOverviewChanged();
        GoToProjectShipments();
    }

    private void GoToProjectShipments()
    {
        SelectedShipment = null;
        IsEditing = false;
        IsCreatingNew = false;
        ShipmentsView.Refresh();
        RaiseViewStateChanged();
    }

    private void GoToProjects()
    {
        SelectedProject = null;
        SelectedShipment = null;
        IsEditing = false;
        IsCreatingNew = false;
        SearchText = string.Empty;
        RebuildProjectSummaries();
        RaiseViewStateChanged();
    }

    private void DeleteShipment(Shipment shipment)
    {
        Shipments.Remove(shipment);
        if (SelectedShipment == shipment) SelectedShipment = null;
        _dataService.SaveShipments(Shipments);
        RaiseOverviewChanged();
        GoToProjectShipments();
    }

    private void ClearDraft()
    {
        DraftProject = null;
        DraftShipmentDate = DateTime.Today;
        DraftSerialNumber = string.Empty;
        DraftModel = string.Empty;
        DraftFirmwareVersion = string.Empty;
        DraftQaStatus = ShipmentQaStatus.Pending;
        DraftLocation = string.Empty;
        DraftRemarks = string.Empty;
    }

    private void RaiseViewStateChanged()
    {
        OnPropertyChanged(nameof(IsProjectsView));
        OnPropertyChanged(nameof(IsProjectShipmentsView));
        OnPropertyChanged(nameof(IsReadOnlyView));
        OnPropertyChanged(nameof(IsFormView));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonLabel));
        OnPropertyChanged(nameof(SelectedProjectShipmentCount));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RaiseOverviewChanged()
    {
        OnPropertyChanged(nameof(TotalProjects));
        OnPropertyChanged(nameof(SelectedProjectShipmentCount));
        OnPropertyChanged(nameof(TotalShipments));
        OnPropertyChanged(nameof(QaPassedCount));
        OnPropertyChanged(nameof(QaReadyCount));
        OnPropertyChanged(nameof(QaBlockedCount));
        OnPropertyChanged(nameof(QaInProgressCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
        OnPropertyChanged(nameof(HasNoProjectSearchResults));
    }
}