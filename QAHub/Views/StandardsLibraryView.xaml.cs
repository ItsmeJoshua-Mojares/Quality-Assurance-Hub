using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using QAHub.ViewModels;

namespace QAHub.Views;

public partial class StandardsLibraryView : UserControl
{
    private bool _coreReady;
    private string? _pendingPdfPath;

    public StandardsLibraryView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is StandardsLibraryViewModel oldVm) oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is StandardsLibraryViewModel vm) vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(StandardsLibraryViewModel.PreviewDocumentPath)) return;

        var path = (DataContext as StandardsLibraryViewModel)?.PreviewDocumentPath;
        if (path == null) return;

        if (_coreReady)
            LoadPdf(path);
        else
        {
            _pendingPdfPath = path;
            EnsureWebView2AndLoad(path);
        }
    }

    private async void EnsureWebView2AndLoad(string path)
    {
        try
        {
            await PdfViewer.EnsureCoreWebView2Async(null);
            _coreReady = true;
            var pending = _pendingPdfPath;
            _pendingPdfPath = null;
            if (pending != null) LoadPdf(pending);
        }
        catch (Exception)
        {
            // WebView2 runtime not available — fall back to the external viewer.
            _coreReady = false;
            _pendingPdfPath = null;
            if (DataContext is StandardsLibraryViewModel viewModel)
                viewModel.OpenExternally(path);
        }
    }

    private void LoadPdf(string path)
    {
        try
        {
            var uri = new Uri(path, UriKind.Absolute);
            if (PdfViewer.Source != uri)
                PdfViewer.Source = uri;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to display the PDF:\n{ex.Message}", "Preview Failed",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not StandardsLibraryViewModel viewModel) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose a standard or reference document",
            Filter = "Documents (*.pdf;*.docx;*.xlsx;*.pptx;*.txt;*.csv)|*.pdf;*.docx;*.xlsx;*.pptx;*.txt;*.csv|PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
            viewModel.DraftFilePath = dialog.FileName;
    }
}