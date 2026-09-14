using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using QAHub.Models;
using QAHub.Services;

namespace QAHub.ViewModels;

public class ProjectsViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    private Project? _selectedProject;
    private bool _isEditing;
    private bool _isCreatingNew;
    private string _searchText = string.Empty;

    // Shared draft fields used by both "create new" and "edit existing" forms
    private string _draftName = string.Empty;
    private string _draftOwner = string.Empty;
    private ProjectStatus _draftStatus = ProjectStatus.Active;
    private string _draftDescription = string.Empty;

    public ProjectsViewModel(IDataService dataService)
    {
        _dataService = dataService;

        foreach (var project in dataService.LoadProjects())
            Projects.Add(project);

        ProjectsView = CollectionViewSource.GetDefaultView(Projects);
        ProjectsView.Filter = FilterProject;

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftName));
        CancelFormCommand = new RelayCommand(_ => GoToList());

        OpenProjectCommand = new RelayCommand(param => { if (param is Project p) OpenReadOnly(p); });
        EditProjectCommand = new RelayCommand(param => { if (param is Project p) OpenEditForm(p); });
        BackToListCommand = new RelayCommand(_ => GoToList());
        DeleteProjectCommand = new RelayCommand(param => { if (param is Project p) DeleteProject(p); });
        ArchiveProjectCommand = new RelayCommand(param => { if (param is Project p) ToggleArchive(p); });
    }

    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>Filtered view of Projects for the list, driven by SearchText (matches name or owner).</summary>
    public ICollectionView ProjectsView { get; }

    public ProjectStatus[] Statuses { get; } = (ProjectStatus[])Enum.GetValues(typeof(ProjectStatus));

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ProjectsView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }
    }

    public Project? SelectedProject
    {
        get => _selectedProject;
        private set => SetProperty(ref _selectedProject, value);
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

    /// <summary>Front page: just the projects list + Create button.</summary>
    public bool IsListView => SelectedProject == null && !IsCreatingNew;

    /// <summary>Create-new or edit-existing form (same layout, different command target).</summary>
    public bool IsFormView => IsCreatingNew || (SelectedProject != null && IsEditing);

    /// <summary>Opened by clicking a row directly — read-only, no edit controls active.</summary>
    public bool IsReadOnlyView => SelectedProject != null && !IsEditing && !IsCreatingNew;

    public string FormTitle => IsCreatingNew ? "New Project" : "Editing Project";
    public string SaveButtonLabel => IsCreatingNew ? "Create Project" : "Save Changes";

    // ---- Draft form fields ----
    public string DraftName
    {
        get => _draftName;
        set { SetProperty(ref _draftName, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string DraftOwner
    {
        get => _draftOwner;
        set => SetProperty(ref _draftOwner, value);
    }

    public ProjectStatus DraftStatus
    {
        get => _draftStatus;
        set => SetProperty(ref _draftStatus, value);
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set => SetProperty(ref _draftDescription, value);
    }

    public int TotalProjects => Projects.Count;
    public int ActiveCount => Projects.Count(p => p.Status == ProjectStatus.Active);
    public int ArchivedCount => Projects.Count(p => p.Status == ProjectStatus.Archived);

    public int FilteredCount => ProjectsView.Cast<object>().Count();
    public bool HasNoSearchResults => TotalProjects > 0 && FilteredCount == 0;

    public ICommand ShowCreateFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand EditProjectCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand DeleteProjectCommand { get; }
    public ICommand ArchiveProjectCommand { get; }

    private bool FilterProject(object obj)
    {
        if (obj is not Project project) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return project.Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (project.Owner?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ShowCreateForm()
    {
        ClearDraft();
        SelectedProject = null;
        IsEditing = false;
        IsCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void OpenReadOnly(Project project)
    {
        SelectedProject = project;
        IsEditing = false;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void OpenEditForm(Project project)
    {
        SelectedProject = project;
        DraftName = project.Name;
        DraftOwner = project.Owner ?? string.Empty;
        DraftStatus = project.Status;
        DraftDescription = project.Description;
        IsEditing = true;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        if (IsCreatingNew)
        {
            var project = new Project
            {
                Id = Projects.Count == 0 ? 1 : Projects.Max(p => p.Id) + 1,
                Name = DraftName.Trim(),
                Owner = string.IsNullOrWhiteSpace(DraftOwner) ? null : DraftOwner.Trim(),
                Status = DraftStatus,
                Description = DraftDescription.Trim()
            };
            Projects.Add(project);
        }
        else if (SelectedProject != null)
        {
            SelectedProject.Name = DraftName.Trim();
            SelectedProject.Owner = string.IsNullOrWhiteSpace(DraftOwner) ? null : DraftOwner.Trim();
            SelectedProject.Status = DraftStatus;
            SelectedProject.Description = DraftDescription.Trim();
        }

        _dataService.SaveProjects(Projects);
        RaiseOverviewChanged();
        GoToList();
    }

    private void GoToList()
    {
        SelectedProject = null;
        IsEditing = false;
        IsCreatingNew = false;
        ClearDraft();
        RaiseViewStateChanged();
    }

    private void DeleteProject(Project project)
    {
        if (MessageBox.Show(
                $"Delete project \"{project.Name}\"? This cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        Projects.Remove(project);
        if (SelectedProject == project) SelectedProject = null;
        _dataService.SaveProjects(Projects);
        RaiseOverviewChanged();
        GoToList();
    }

    private void ToggleArchive(Project project)
    {
        project.Status = project.Status == ProjectStatus.Active ? ProjectStatus.Archived : ProjectStatus.Active;
        _dataService.SaveProjects(Projects);
        RaiseOverviewChanged();
    }

    private void ClearDraft()
    {
        DraftName = string.Empty;
        DraftOwner = string.Empty;
        DraftStatus = ProjectStatus.Active;
        DraftDescription = string.Empty;
    }

    private void RaiseViewStateChanged()
    {
        OnPropertyChanged(nameof(IsListView));
        OnPropertyChanged(nameof(IsFormView));
        OnPropertyChanged(nameof(IsReadOnlyView));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonLabel));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RaiseOverviewChanged()
    {
        OnPropertyChanged(nameof(TotalProjects));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(ArchivedCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
    }
}
