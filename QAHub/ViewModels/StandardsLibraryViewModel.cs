using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using QAHub.Models;
using QAHub.Services;

namespace QAHub.ViewModels;

public class StandardsLibraryViewModel : ObservableObject
{
    private readonly IDataService _dataService;

    private string _searchText = string.Empty;
    private CategoryFilterOption? _categoryFilter = new("All categories", null);
    private bool _isFormView;

    private StandardDocument? _editingDocument;
    private StandardDocument? _previewDocument;
    private string? _previewDocumentPath;

    private string _draftTitle = string.Empty;
    private StandardCategory _draftCategory = StandardCategory.Other;
    private string _draftDescription = string.Empty;
    private string _draftFilePath = string.Empty;

    public StandardsLibraryViewModel(IDataService dataService)
    {
        _dataService = dataService;

        foreach (var document in dataService.LoadStandardDocuments())
            Documents.Add(document);

        DocumentsView = CollectionViewSource.GetDefaultView(Documents);
        DocumentsView.Filter = FilterDocument;

        ShowRegisterFormCommand = new RelayCommand(_ => ShowRegisterForm());
        SaveFormCommand = new RelayCommand(_ => SaveForm(), _ => CanSaveForm());
        CancelFormCommand = new RelayCommand(_ => GoToList());
        OpenDocumentCommand = new RelayCommand(param => { if (param is StandardDocument d) OpenDocument(d); });
        EditDocumentCommand = new RelayCommand(param => { if (param is StandardDocument d) ShowEditForm(d); });
        DeleteDocumentCommand = new RelayCommand(param => { if (param is StandardDocument d) DeleteDocument(d); });
        BackToListCommand = new RelayCommand(_ => GoToList());
    }

    public ObservableCollection<StandardDocument> Documents { get; } = new();

    public ICollectionView DocumentsView { get; }

    public StandardCategory[] AllCategories { get; } = (StandardCategory[])Enum.GetValues(typeof(StandardCategory));

    /// <summary>Category filter choices — the leading option (null value) represents "All categories".</summary>
    public CategoryFilterOption[] FilterOptions { get; } =
        new[] { new CategoryFilterOption("All categories", null) }
            .Concat(((StandardCategory[])Enum.GetValues(typeof(StandardCategory)))
                .Select(c => new CategoryFilterOption(c.GetDisplayName(), c)))
            .ToArray();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                DocumentsView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }
    }

    public CategoryFilterOption? CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (SetProperty(ref _categoryFilter, value))
            {
                DocumentsView.Refresh();
                OnPropertyChanged(nameof(FilteredCount));
                OnPropertyChanged(nameof(HasNoSearchResults));
            }
        }
    }

    public StandardDocument? PreviewDocument
    {
        get => _previewDocument;
        private set
        {
            if (SetProperty(ref _previewDocument, value))
            {
                OnPropertyChanged(nameof(IsPreviewView));
                OnPropertyChanged(nameof(IsListView));
                OnPropertyChanged(nameof(IsListOrForm));
            }
        }
    }

    public string? PreviewDocumentPath
    {
        get => _previewDocumentPath;
        private set => SetProperty(ref _previewDocumentPath, value);
    }

    public bool IsPreviewView => _previewDocument != null;
    public bool IsFormView => _isFormView;
    public bool IsListView => !_isFormView && _previewDocument == null;
    public bool IsListOrForm => !IsPreviewView;

    public string FormTitle => _editingDocument == null ? "Register Document" : "Edit Document";
    public string SaveButtonLabel => _editingDocument == null ? "Register" : "Save Changes";

    // ---- Draft form fields ----
    public string DraftTitle
    {
        get => _draftTitle;
        set { SetProperty(ref _draftTitle, value); CommandManager.InvalidateRequerySuggested(); }
    }

    public StandardCategory DraftCategory
    {
        get => _draftCategory;
        set => SetProperty(ref _draftCategory, value);
    }

    public string DraftDescription
    {
        get => _draftDescription;
        set => SetProperty(ref _draftDescription, value);
    }

    public string DraftFilePath
    {
        get => _draftFilePath;
        set
        {
            if (SetProperty(ref _draftFilePath, value))
            {
                OnPropertyChanged(nameof(SelectedFileName));
                OnPropertyChanged(nameof(FileHintText));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>Original name of the file selected for upload (or of the attached file while editing).</summary>
    public string SelectedFileName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_draftFilePath))
                return Path.GetFileName(_draftFilePath);
            return _editingDocument?.FileName ?? string.Empty;
        }
    }

    public string FileHintText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_draftFilePath))
                return Path.GetFileName(_draftFilePath);

            if (_editingDocument != null)
                return $"Current file: {_editingDocument.FileName} ({_editingDocument.SizeDisplay})";

            return "No file chosen yet. PDF files open in-app; other files open in their default app.";
        }
    }

    public int TotalDocuments => Documents.Count;
    public int FilteredCount => DocumentsView.Cast<object>().Count();
    public bool HasNoSearchResults => TotalDocuments > 0 && FilteredCount == 0;

    public ICommand ShowRegisterFormCommand { get; }
    public ICommand SaveFormCommand { get; }
    public ICommand CancelFormCommand { get; }
    public ICommand OpenDocumentCommand { get; }
    public ICommand EditDocumentCommand { get; }
    public ICommand DeleteDocumentCommand { get; }
    public ICommand BackToListCommand { get; }

    private bool CanSaveForm()
        => !string.IsNullOrWhiteSpace(DraftTitle)
           && (_editingDocument != null || !string.IsNullOrWhiteSpace(DraftFilePath));

    private bool FilterDocument(object obj)
    {
        if (obj is not StandardDocument document) return false;

        var category = CategoryFilter?.Value;
        if (category.HasValue && document.Category != category.Value)
            return false;

        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var needle = SearchText.Trim();
        return document.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || document.Description.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || document.FileName.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private void ShowRegisterForm()
    {
        ClearDraft();
        _editingDocument = null;
        _isFormView = true;
        RaiseViewStateChanged();
    }

    private void ShowEditForm(StandardDocument document)
    {
        _editingDocument = document;
        DraftTitle = document.Title;
        DraftCategory = document.Category;
        DraftDescription = document.Description;
        DraftFilePath = string.Empty;
        _isFormView = true;
        OnPropertyChanged(nameof(FileHintText));
        RaiseViewStateChanged();
    }

    private void SaveForm()
    {
        if (_editingDocument == null)
        {
            var document = _dataService.StoreDocumentFile(_draftFilePath, DraftTitle.Trim(), DraftDescription.Trim(), DraftCategory);
            document.Id = Documents.Count == 0 ? 1 : Documents.Max(d => d.Id) + 1;
            Documents.Add(document);
        }
        else
        {
            var document = _editingDocument;
            document.Title = DraftTitle.Trim();
            document.Description = DraftDescription.Trim();
            document.Category = DraftCategory;

            if (!string.IsNullOrWhiteSpace(_draftFilePath))
            {
                _dataService.DeleteDocumentFile(document);
                var replacement = _dataService.StoreDocumentFile(_draftFilePath, DraftTitle.Trim(), DraftDescription.Trim(), DraftCategory);
                document.FileName = replacement.FileName;
                document.StoredName = replacement.StoredName;
                document.Extension = replacement.Extension;
                document.SizeBytes = replacement.SizeBytes;
            }
        }

        _dataService.SaveStandardDocuments(Documents);
        OnPropertyChanged(nameof(TotalDocuments));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
        GoToList();
    }

    private void OpenDocument(StandardDocument document)
    {
        var path = _dataService.GetDocumentPath(document);
        if (path == null)
        {
            MessageBox.Show("The stored file for this document could not be found.", "File Not Found",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (document.IsPdf)
        {
            PreviewDocument = document;
            PreviewDocumentPath = path;
        }
        else
        {
            OpenExternally(path);
        }
    }

    /// <summary>Opens a document in its default external app (used for non-PDF files and viewer fallbacks).</summary>
    public void OpenExternally(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to open the file:\n{ex.Message}", "Open Failed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DeleteDocument(StandardDocument document)
    {
        var result = MessageBox.Show(
            $"Delete \"{document.Title}\" and its stored file? This cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        _dataService.DeleteDocumentFile(document);
        Documents.Remove(document);
        if (PreviewDocument == document)
        {
            PreviewDocument = null;
            PreviewDocumentPath = null;
        }

        _dataService.SaveStandardDocuments(Documents);
        OnPropertyChanged(nameof(TotalDocuments));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(HasNoSearchResults));
    }

    private void GoToList()
    {
        _editingDocument = null;
        _isFormView = false;
        PreviewDocument = null;
        PreviewDocumentPath = null;
        ClearDraft();
        RaiseViewStateChanged();
    }

    private void ClearDraft()
    {
        DraftTitle = string.Empty;
        DraftCategory = StandardCategory.Other;
        DraftDescription = string.Empty;
        DraftFilePath = string.Empty;
    }

    private void RaiseViewStateChanged()
    {
        OnPropertyChanged(nameof(IsListView));
        OnPropertyChanged(nameof(IsFormView));
        OnPropertyChanged(nameof(IsPreviewView));
        OnPropertyChanged(nameof(IsListOrForm));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(SaveButtonLabel));
        OnPropertyChanged(nameof(FileHintText));
        CommandManager.InvalidateRequerySuggested();
    }
}