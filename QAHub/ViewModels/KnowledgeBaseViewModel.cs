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

public class KnowledgeBaseViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    private KnowledgeArticle? _selectedArticle;
    private bool _isEditingArticle;
    private bool _isCreatingNew;
    private string _searchText = string.Empty;

    // Shared draft fields used by both "create new" and "edit existing" forms
    private string _draftTitle = string.Empty;
    private KnowledgeCategory _draftCategory = KnowledgeCategory.Other;
    private string _draftTags = string.Empty;
    private string _draftContent = string.Empty;

    public KnowledgeBaseViewModel(IDataService dataService)
    {
        _dataService = dataService;

        foreach (var article in dataService.LoadKnowledgeArticles())
            Articles.Add(article);

        ArticlesView = CollectionViewSource.GetDefaultView(Articles);
        ArticlesView.Filter = FilterArticle;

        ShowCreateFormCommand = new RelayCommand(_ => ShowCreateForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => !string.IsNullOrWhiteSpace(DraftTitle));
        CancelFormCommand = new RelayCommand(_ => CancelForm());

        OpenArticleCommand = new RelayCommand(param => { if (param is KnowledgeArticle a) OpenReadOnly(a); });
        EditArticleCommand = new RelayCommand(param => { if (param is KnowledgeArticle a) OpenEditForm(a); });
        BackToListCommand = new RelayCommand(_ => GoToList());
        DeleteArticleCommand = new RelayCommand(param => { if (param is KnowledgeArticle a) DeleteArticle(a); });
    }

    public ObservableCollection<KnowledgeArticle> Articles { get; } = new();

    /// <summary>Filtered view of Articles for the list, driven by SearchText (matches title or tags).</summary>
    public ICollectionView ArticlesView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ArticlesView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }
    }

    public KnowledgeCategory[] Categories { get; } = (KnowledgeCategory[])Enum.GetValues(typeof(KnowledgeCategory));

    public KnowledgeArticle? SelectedArticle
    {
        get => _selectedArticle;
        private set => SetProperty(ref _selectedArticle, value);
    }

    private bool IsEditingArticle
    {
        get => _isEditingArticle;
        set => SetProperty(ref _isEditingArticle, value);
    }

    private bool IsCreatingNew
    {
        get => _isCreatingNew;
        set => SetProperty(ref _isCreatingNew, value);
    }

    /// <summary>Front page: just the article list + Create button.</summary>
    public bool IsListView => SelectedArticle == null && !IsCreatingNew;

    /// <summary>Create-new or edit-existing form (same layout, different command target).</summary>
    public bool IsFormView => IsCreatingNew || (SelectedArticle != null && IsEditingArticle);

    /// <summary>Opened by clicking a row directly — read-only, no edit controls active.</summary>
    public bool IsReadOnlyView => SelectedArticle != null && !IsEditingArticle && !IsCreatingNew;

    public string FormTitle => IsCreatingNew ? "New Article" : "Editing Article";
    public string SaveButtonLabel => IsCreatingNew ? "Create Article" : "Save Changes";

    // ---- Draft form fields ----
    public string DraftTitle
    {
        get => _draftTitle;
        set { SetProperty(ref _draftTitle, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public KnowledgeCategory DraftCategory
    {
        get => _draftCategory;
        set => SetProperty(ref _draftCategory, value);
    }

    public string DraftTags
    {
        get => _draftTags;
        set => SetProperty(ref _draftTags, value);
    }

    public string DraftContent
    {
        get => _draftContent;
        set => SetProperty(ref _draftContent, value);
    }

    public int TotalArticles => Articles.Count;
    public int FilteredCount => ArticlesView.Cast<object>().Count();
    public bool HasNoSearchResults => TotalArticles > 0 && FilteredCount == 0;

    public ICommand ShowCreateFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand OpenArticleCommand { get; }
    public ICommand EditArticleCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand DeleteArticleCommand { get; }

    private bool FilterArticle(object obj)
    {
        if (obj is not KnowledgeArticle article) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return article.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || (article.Tags?.Contains(needle, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void ShowCreateForm()
    {
        ClearDraft();
        SelectedArticle = null;
        IsEditingArticle = false;
        IsCreatingNew = true;
        RaiseViewStateChanged();
    }

    private void OpenReadOnly(KnowledgeArticle article)
    {
        SelectedArticle = article;
        IsEditingArticle = false;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void OpenEditForm(KnowledgeArticle article)
    {
        SelectedArticle = article;
        DraftTitle = article.Title;
        DraftCategory = article.Category;
        DraftTags = article.Tags ?? string.Empty;
        DraftContent = article.Content;
        IsEditingArticle = true;
        IsCreatingNew = false;
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        if (IsCreatingNew)
        {
            var article = new KnowledgeArticle
            {
                Id = Articles.Count == 0 ? 1 : Articles.Max(a => a.Id) + 1,
                Title = DraftTitle.Trim(),
                Category = DraftCategory,
                Tags = string.IsNullOrWhiteSpace(DraftTags) ? null : DraftTags.Trim(),
                Content = DraftContent.Trim()
            };
            Articles.Add(article);
        }
        else if (SelectedArticle != null)
        {
            SelectedArticle.Title = DraftTitle.Trim();
            SelectedArticle.Category = DraftCategory;
            SelectedArticle.Tags = string.IsNullOrWhiteSpace(DraftTags) ? null : DraftTags.Trim();
            SelectedArticle.Content = DraftContent.Trim();
        }

        _dataService.SaveKnowledgeArticles(Articles);
        OnPropertyChanged(nameof(TotalArticles));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
        GoToList();
    }

    private void CancelForm() => GoToList();

    private void GoToList()
    {
        SelectedArticle = null;
        IsEditingArticle = false;
        IsCreatingNew = false;
        ClearDraft();
        RaiseViewStateChanged();
    }

    private void DeleteArticle(KnowledgeArticle article)
    {
        if (MessageBox.Show(
                $"Delete article \"{article.Title}\"? This cannot be undone.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        Articles.Remove(article);
        if (SelectedArticle == article) SelectedArticle = null;
        _dataService.SaveKnowledgeArticles(Articles);
        OnPropertyChanged(nameof(TotalArticles));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
        GoToList();
    }

    private void ClearDraft()
    {
        DraftTitle = string.Empty;
        DraftCategory = KnowledgeCategory.Other;
        DraftTags = string.Empty;
        DraftContent = string.Empty;
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
}