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

/// <summary>A Test Case wrapped with a checkbox state, used by the "Covered By" multi-select in the Requirement form.</summary>
public class SelectableTestCase : ObservableObject
{
    private bool _isSelected;

    public SelectableTestCase(TestCase testCase, bool isSelected)
    {
        TestCase = testCase;
        _isSelected = isSelected;
    }

    public TestCase TestCase { get; }
    public string Title => TestCase.Title;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class RequirementsViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly ObservableCollection<TestCase> _availableTestCases;

    private Requirement? _selectedRequirement;
    private bool _isEditing;
    private bool _isCreatingNew;
    private string _searchText = string.Empty;

    // Shared draft fields used by both "create new" and "edit existing" forms
    private string _draftTitle = string.Empty;
    private string _draftSource = string.Empty;
    private RequirementCoverageStatus _draftCoverageStatus = RequirementCoverageStatus.Untested;
    private string _draftDescription = string.Empty;

    public RequirementsViewModel(IDataService dataService, ObservableCollection<TestCase> availableTestCases)
    {
        _dataService = dataService;
        _availableTestCases = availableTestCases;

        foreach (var requirement in dataService.LoadRequirements())
            Requirements.Add(requirement);

        RequirementsView = CollectionViewSource.GetDefaultView(Requirements);
        RequirementsView.Filter = FilterRequirement;

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftTitle));
        CancelFormCommand = new RelayCommand(_ => GoToList());

        OpenRequirementCommand = new RelayCommand(param => { if (param is Requirement r) OpenReadOnly(r); });
        EditRequirementCommand = new RelayCommand(param => { if (param is Requirement r) OpenEditForm(r); });
        BackToListCommand = new RelayCommand(_ => GoToList());
        DeleteRequirementCommand = new RelayCommand(param => { if (param is Requirement r) DeleteRequirement(r); });
    }

    public ObservableCollection<Requirement> Requirements { get; } = new();

    /// <summary>Filtered view of Requirements for the list, driven by SearchText (matches title or source).</summary>
    public ICollectionView RequirementsView { get; }

    public RequirementCoverageStatus[] CoverageStatuses { get; } =
        (RequirementCoverageStatus[])Enum.GetValues(typeof(RequirementCoverageStatus));

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                RequirementsView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }
    }

    public Requirement? SelectedRequirement
    {
        get => _selectedRequirement;
        private set
        {
            if (SetProperty(ref _selectedRequirement, value))
            {
                OnPropertyChanged(nameof(LinkedTestCaseNamesDisplay));
            }
        }
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

    /// <summary>Front page: just the requirements list + Create button.</summary>
    public bool IsListView => SelectedRequirement == null && !IsCreatingNew;

    /// <summary>Create-new or edit-existing form (same layout, different command target).</summary>
    public bool IsFormView => IsCreatingNew || (SelectedRequirement != null && IsEditing);

    /// <summary>Opened by clicking a row directly — read-only, no edit controls active.</summary>
    public bool IsReadOnlyView => SelectedRequirement != null && !IsEditing && !IsCreatingNew;

    public string FormTitle => IsCreatingNew ? "New Requirement" : "Editing Requirement";
    public string SaveButtonLabel => IsCreatingNew ? "Create Requirement" : "Save Changes";

    /// <summary>Resolved test case titles for the currently open (read-only) requirement, for display.</summary>
    public string LinkedTestCaseNamesDisplay
    {
        get
        {
            if (SelectedRequirement == null || SelectedRequirement.LinkedTestCaseIds.Count == 0)
                return "No test cases linked yet";

            var names = SelectedRequirement.LinkedTestCaseIds
                .Select(id => _availableTestCases.FirstOrDefault(tc => tc.Id == id)?.Title)
                .Where(title => title != null);

            return string.Join(", ", names);
        }
    }

    // ---- Draft form fields ----
    public string DraftTitle
    {
        get => _draftTitle;
        set { SetProperty(ref _draftTitle, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public string DraftSource
    {
        get => _draftSource;
        set => SetProperty(ref _draftSource, value);
    }

    public RequirementCoverageStatus DraftCoverageStatus
    {
        get => _draftCoverageStatus;
        set => SetProperty(ref _draftCoverageStatus, value);
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set => SetProperty(ref _draftDescription, value);
    }

    /// <summary>Every available test case with a checkbox state, rebuilt each time the form opens.</summary>
    public ObservableCollection<SelectableTestCase> DraftLinkedTestCases { get; } = new();

    public int TotalRequirements => Requirements.Count;
    public int FullyTestedCount => Requirements.Count(r => r.CoverageStatus == RequirementCoverageStatus.FullyTested);
    public int PartiallyTestedCount => Requirements.Count(r => r.CoverageStatus == RequirementCoverageStatus.PartiallyTested);
    public int UntestedCount => Requirements.Count(r => r.CoverageStatus == RequirementCoverageStatus.Untested);

    public int FilteredCount => RequirementsView.Cast<object>().Count();
    public bool HasNoSearchResults => TotalRequirements > 0 && FilteredCount == 0;

    public ICommand ShowCreateFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand OpenRequirementCommand { get; }
    public ICommand EditRequirementCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand DeleteRequirementCommand { get; }

    private bool FilterRequirement(object obj)
    {
        if (obj is not Requirement requirement) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return requirement.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (requirement.Source?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ShowCreateForm()
    {
        ClearDraft();
        RebuildDraftLinkedTestCases(selectedIds: null);
        SelectedRequirement = null;
        IsEditing = false;
        IsCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void OpenReadOnly(Requirement requirement)
    {
        SelectedRequirement = requirement;
        IsEditing = false;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void OpenEditForm(Requirement requirement)
    {
        SelectedRequirement = requirement;
        DraftTitle = requirement.Title;
        DraftSource = requirement.Source ?? string.Empty;
        DraftCoverageStatus = requirement.CoverageStatus;
        DraftDescription = requirement.Description;
        RebuildDraftLinkedTestCases(requirement.LinkedTestCaseIds);
        IsEditing = true;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    /// <summary>Rebuilds the checkbox list against the current AvailableTestCases, checking off any ids already linked.</summary>
    private void RebuildDraftLinkedTestCases(System.Collections.Generic.List<int>? selectedIds)
    {
        DraftLinkedTestCases.Clear();
        foreach (var testCase in _availableTestCases)
        {
            var isSelected = selectedIds?.Contains(testCase.Id) ?? false;
            DraftLinkedTestCases.Add(new SelectableTestCase(testCase, isSelected));
        }
    }

    private void SaveForm()
    {
        var linkedIds = DraftLinkedTestCases.Where(x => x.IsSelected).Select(x => x.TestCase.Id).ToList();

        if (IsCreatingNew)
        {
            var requirement = new Requirement
            {
                Id = Requirements.Count == 0 ? 1 : Requirements.Max(r => r.Id) + 1,
                Title = DraftTitle.Trim(),
                Source = string.IsNullOrWhiteSpace(DraftSource) ? null : DraftSource.Trim(),
                CoverageStatus = DraftCoverageStatus,
                LinkedTestCaseIds = linkedIds,
                Description = DraftDescription.Trim()
            };
            Requirements.Add(requirement);
        }
        else if (SelectedRequirement != null)
        {
            SelectedRequirement.Title = DraftTitle.Trim();
            SelectedRequirement.Source = string.IsNullOrWhiteSpace(DraftSource) ? null : DraftSource.Trim();
            SelectedRequirement.CoverageStatus = DraftCoverageStatus;
            SelectedRequirement.LinkedTestCaseIds = linkedIds;
            SelectedRequirement.Description = DraftDescription.Trim();
        }

        _dataService.SaveRequirements(Requirements);
        RaiseOverviewChanged();
        GoToList();
    }

    private void GoToList()
    {
        SelectedRequirement = null;
        IsEditing = false;
        IsCreatingNew = false;
        ClearDraft();
        RaiseViewStateChanged();
    }

    private void DeleteRequirement(Requirement requirement)
    {
        if (MessageBox.Show(
                $"Delete requirement \"{requirement.Title}\"? This cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        Requirements.Remove(requirement);
        if (SelectedRequirement == requirement) SelectedRequirement = null;
        _dataService.SaveRequirements(Requirements);
        RaiseOverviewChanged();
        GoToList();
    }

    private void ClearDraft()
    {
        DraftTitle = string.Empty;
        DraftSource = string.Empty;
        DraftCoverageStatus = RequirementCoverageStatus.Untested;
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
        OnPropertyChanged(nameof(TotalRequirements));
        OnPropertyChanged(nameof(FullyTestedCount));
        OnPropertyChanged(nameof(PartiallyTestedCount));
        OnPropertyChanged(nameof(UntestedCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
    }

    /// <summary>Bulk-clears without the per-item confirmation dialog — see TestRunsViewModel.ClearAll for why.</summary>
    public void ClearAll()
    {
        Requirements.Clear();
        SelectedRequirement = null;
        _dataService.SaveRequirements(Requirements);
        RaiseOverviewChanged();
    }
}
