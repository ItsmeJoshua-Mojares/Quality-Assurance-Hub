using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using QAHub.Services;

namespace QAHub.ViewModels;

/// <summary>A single row in the Storage stats table.</summary>
public class StorageRow
{
    public string Section { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public long FileSizeBytes { get; set; }
    public string FileSizeLabel { get; set; } = string.Empty;
}

public class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;
    private readonly IDataService _dataService;

    private int _totalRecordCount;
    private string _totalFileSizeLabel = "0 B";

    public SettingsViewModel(MainViewModel mainViewModel, IDataService dataService)
    {
        _mainViewModel = mainViewModel;
        _dataService = dataService;

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

        OpenDataFolderCommand = new RelayCommand(_ => OpenDataFolder());
        ExportDataCommand = new RelayCommand(_ => ExportData());
        ImportDataCommand = new RelayCommand(_ => ImportData());
        RefreshStorageStatsCommand = new RelayCommand(_ => RefreshStorageStats());
    }

    public string DataDirectoryPath => _dataService.DataDirectory;

    public ObservableCollection<StorageRow> StorageRows { get; } = new();
    public int TotalRecordCount { get => _totalRecordCount; private set => SetProperty(ref _totalRecordCount, value); }
    public string TotalFileSizeLabel { get => _totalFileSizeLabel; private set => SetProperty(ref _totalFileSizeLabel, value); }

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

    public ICommand OpenDataFolderCommand { get; }
    public ICommand ExportDataCommand { get; }
    public ICommand ImportDataCommand { get; }
    public ICommand RefreshStorageStatsCommand { get; }

    private bool Confirm(string message)
    {
        return MessageBox.Show(message, "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            == MessageBoxResult.Yes;
    }

    public void RefreshStorageStats()
    {
        StorageRows.Clear();

        AddStorageRow("Test Cases", _mainViewModel.TestCases.Items.Count, "testcases.json");
        AddStorageRow("Bugs", _mainViewModel.Bugs.Items.Count, "bugs.json");
        AddStorageRow("Test Runs", _mainViewModel.TestRuns.Runs.Count, "testruns.json");
        AddStorageRow("Requirements", _mainViewModel.Requirements.Requirements.Count, "requirements.json");
        AddStorageRow("Projects", _mainViewModel.Projects.Projects.Count, "projects.json");
        AddStorageRow("Shipment Tracker", _mainViewModel.Shipments.Shipments.Count, "shipments.json");
        AddStorageRow("CAPAR Tracker", _mainViewModel.Capars.Items.Count, "capar.json");
        AddStorageRow("RMA Tracker", _mainViewModel.Rmas.Items.Count, "rma.json");
        AddStorageRow("Knowledge Base", _mainViewModel.KnowledgeBase.Articles.Count, "knowledgebase.json");

        TotalRecordCount = StorageRows.Sum(r => r.RecordCount);
        TotalFileSizeLabel = FormatBytes(StorageRows.Sum(r => r.FileSizeBytes));
    }

    private void AddStorageRow(string label, int recordCount, string fileName)
    {
        var path = Path.Combine(_dataService.DataDirectory, fileName);
        var sizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0L;

        StorageRows.Add(new StorageRow
        {
            Section = label,
            RecordCount = recordCount,
            FileSizeBytes = sizeBytes,
            FileSizeLabel = FormatBytes(sizeBytes)
        });
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024):0.#} MB";
    }

    private void OpenDataFolder()
    {
        try
        {
            Directory.CreateDirectory(_dataService.DataDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = _dataService.DataDirectory,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Couldn't open the data folder:\n{ex.Message}", "Open Data Folder",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportData()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Zip Archive (*.zip)|*.zip",
            FileName = $"qahub-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            if (File.Exists(dialog.FileName)) File.Delete(dialog.FileName);
            ZipFile.CreateFromDirectory(_dataService.DataDirectory, dialog.FileName);
            MessageBox.Show("Backup created successfully.", "Export Data",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed:\n{ex.Message}", "Export Data",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportData()
    {
        if (!Confirm(
                "Importing will overwrite your current data files with the contents of the " +
                "selected backup. You'll need to restart QA Hub afterward for the restored " +
                "data to load. Continue?"))
            return;

        var dialog = new OpenFileDialog { Filter = "Zip Archive (*.zip)|*.zip" };
        if (dialog.ShowDialog() != true) return;

        try
        {
            ZipFile.ExtractToDirectory(dialog.FileName, _dataService.DataDirectory, overwriteFiles: true);
            RefreshStorageStats();
            MessageBox.Show("Backup restored. Please restart QA Hub for the changes to take effect.",
                "Import Data", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Import failed:\n{ex.Message}", "Import Data",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ResetTestCases()
    {
        if (!Confirm("Delete all test cases? This cannot be undone.")) return;
        DeleteAllTestCases();
        RefreshStorageStats();
    }

    private void ResetBugs()
    {
        if (!Confirm("Delete all bugs? This cannot be undone.")) return;
        DeleteAllBugs();
        RefreshStorageStats();
    }

    private void ResetTestRuns()
    {
        if (!Confirm("Delete all test runs? This cannot be undone.")) return;
        DeleteAllTestRuns();
        RefreshStorageStats();
    }

    private void ResetRequirements()
    {
        if (!Confirm("Delete all requirements? This cannot be undone.")) return;
        DeleteAllRequirements();
        RefreshStorageStats();
    }

    private void ResetProjects()
    {
        if (!Confirm("Delete all projects? This cannot be undone.")) return;
        DeleteAllProjects();
        RefreshStorageStats();
    }

    private void ResetShipments()
    {
        if (!Confirm("Delete all shipments? This cannot be undone.")) return;
        DeleteAllShipments();
        RefreshStorageStats();
    }

    private void ResetCapars()
    {
        if (!Confirm("Delete all CAPARs? This cannot be undone.")) return;
        DeleteAllCapars();
        RefreshStorageStats();
    }

    private void ResetRmas()
    {
        if (!Confirm("Delete all RMAs? This cannot be undone.")) return;
        DeleteAllRmas();
        RefreshStorageStats();
    }

    private void ResetKnowledgeBase()
    {
        if (!Confirm("Delete all knowledge base articles? This cannot be undone.")) return;
        DeleteAllKnowledgeArticles();
        RefreshStorageStats();
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
        RefreshStorageStats();
    }

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

    private void DeleteAllTestRuns() => _mainViewModel.TestRuns.ClearAll();
    private void DeleteAllRequirements() => _mainViewModel.Requirements.ClearAll();
    private void DeleteAllProjects() => _mainViewModel.Projects.ClearAll();
    private void DeleteAllKnowledgeArticles() => _mainViewModel.KnowledgeBase.ClearAll();

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
}