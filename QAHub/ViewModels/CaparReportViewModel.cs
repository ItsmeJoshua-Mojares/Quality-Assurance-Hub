using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using QAHub.Models;

namespace QAHub.ViewModels;

/// <summary>
/// A snapshot row for the "CAPAR Register" report page — a flattened view of
/// the fields entered on the New CAPAR form, rebuilt fresh on each Refresh().
/// Nothing is stored: every row is derived live from the real CAPAR records.
/// </summary>
public class CaparReportRow
{
    public string CaparNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public CaparSource Source { get; set; }
    public string Project { get; set; } = "Unassigned";
    public Severity Severity { get; set; }
    public CaparStatus Status { get; set; }
    public string Owner { get; set; } = "Not assigned";
    public DateTime OpenedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string RootCause { get; set; } = string.Empty;
    public string CorrectiveAction { get; set; } = string.Empty;
    public string PreventiveAction { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}

/// <summary>
/// Live, read-only register of corrective &amp; preventive actions for the
/// Reports page. Open CAPARs appear automatically; once a CAPAR is Closed it
/// drops off this page unless ShowClosed is checked. Data is always read from
/// the CAPAR Tracker's live collection — never stored twice.
/// </summary>
public class CaparReportViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    private bool _showClosed;
    private CaparReportRow? _selected;

    public CaparReportViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        RefreshCommand = new RelayCommand(_ => Refresh());
        SelectItemCommand = new RelayCommand(param => { if (param is CaparReportRow row) Selected = row; });
        PrintRowCommand = new RelayCommand(param => { if (param is CaparReportRow row) Print(row); });
        Refresh();
    }

    public ObservableCollection<CaparReportRow> Rows { get; } = new();

    public string ShownCountLabel => Rows.Count == 1 ? "1 open CAPAR" : $"{Rows.Count} open CAPAR(s)";

    public bool ShowClosed
    {
        get => _showClosed;
        set
        {
            if (SetProperty(ref _showClosed, value))
                Refresh();
        }
    }

    public CaparReportRow? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
                OnPropertyChanged(nameof(HasSelection));
        }
    }

    public bool HasSelection => Selected != null;

    // ---- Overview stats (recomputed from the filtered rows on every Refresh) ----
    public int ActiveCount { get; private set; }
    public int ImplementedCount { get; private set; }
    public int VerifiedCount { get; private set; }
    public int CriticalCount { get; private set; }
    public int OverdueCount { get; private set; }

    public ICommand RefreshCommand { get; }
    public ICommand SelectItemCommand { get; }
    public ICommand PrintRowCommand { get; }

    /// <summary>
    /// Rebuilds the register from the live CAPAR Tracker collection, dropping
    /// Closed items unless ShowClosed is checked — this is what makes a newly
    /// filed CAPAR appear here and a newly Closed one vanish.
    /// </summary>
    public void Refresh()
    {
        var allCapars = _mainViewModel.Capars.Items;
        var capars = !ShowClosed
            ? allCapars.Where(c => c.Status != CaparStatus.Closed)
            : allCapars;

        Rows.Clear();
        foreach (var capar in capars
            .OrderBy(c => c.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(c => c.OpenedDate))
        {
            Rows.Add(new CaparReportRow
            {
                CaparNumber = capar.CaparNumber,
                Title = capar.Title,
                Source = capar.Source,
                Project = string.IsNullOrWhiteSpace(capar.Project) ? "Unassigned" : capar.Project,
                Severity = capar.Severity,
                Status = capar.Status,
                Owner = string.IsNullOrWhiteSpace(capar.Owner) ? "Not assigned" : capar.Owner,
                OpenedDate = capar.OpenedDate,
                DueDate = capar.DueDate,
                ClosedDate = capar.ClosedDate,
                RootCause = capar.RootCause,
                CorrectiveAction = capar.CorrectiveAction,
                PreventiveAction = capar.PreventiveAction,
                Remarks = capar.Remarks ?? string.Empty
            });
        }

        ActiveCount = Rows.Count(r => r.Status is CaparStatus.Open or CaparStatus.InProgress);
        ImplementedCount = Rows.Count(r => r.Status == CaparStatus.Implemented);
        VerifiedCount = Rows.Count(r => r.Status == CaparStatus.Verified);
        CriticalCount = Rows.Count(r => r.Severity == Severity.Critical);
        OverdueCount = Rows.Count(r => r.Status != CaparStatus.Closed
            && r.DueDate.HasValue && r.DueDate.Value.Date < DateTime.Today);

        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(ImplementedCount));
        OnPropertyChanged(nameof(VerifiedCount));
        OnPropertyChanged(nameof(CriticalCount));
        OnPropertyChanged(nameof(OverdueCount));
        OnPropertyChanged(nameof(ShownCountLabel));

        if (Selected != null && !Rows.Contains(Selected))
            Selected = null;
    }

    /// <summary>
    /// Renders the selected CAPAR as a professional single-page document and
    /// prints it at exact 1:1 scale — no fit-to-page. The layout is a plain
    /// visual tree sized to the printer's printable area, so Microsoft Print
    /// to PDF and physical printers receive the full-size template.
    /// </summary>
    private static void Print(CaparReportRow row)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
        var area = capabilities.PageImageableArea;

        const double fallbackSheetWidth = 816;   // US Letter in DIPs (1/96")
        const double fallbackSheetHeight = 1056;
        const double fallbackMargin = 48;

        double sheetWidth, sheetHeight, originX, originY, areaWidth, areaHeight;
        if (area != null)
        {
            originX = area.OriginWidth;
            originY = area.OriginHeight;
            areaWidth = area.ExtentWidth;
            areaHeight = area.ExtentHeight;
            sheetWidth = originX * 2 + areaWidth;
            sheetHeight = originY * 2 + areaHeight;
        }
        else
        {
            originX = originY = fallbackMargin;
            areaWidth = fallbackSheetWidth - fallbackMargin * 2;
            areaHeight = fallbackSheetHeight - fallbackMargin * 2;
            sheetWidth = fallbackSheetWidth;
            sheetHeight = fallbackSheetHeight;
        }

        var report = BuildReport(row, areaWidth, areaHeight);

        var canvas = new Canvas
        {
            Width = sheetWidth,
            Height = sheetHeight,
            Background = Brushes.White
        };
        canvas.Children.Add(report);
        Canvas.SetLeft(report, originX);
        Canvas.SetTop(report, originY);

        dialog.PrintVisual(canvas, $"CAPAR {row.CaparNumber}");
    }

    /// <summary>
    /// Assembles the full-page layout: letterhead on top, footer pinned to the
    /// bottom, and the body filling the middle (clipped if it overflows).
    /// </summary>
    private static UIElement BuildReport(CaparReportRow row, double width, double height)
    {
        const double padding = 40;

        var body = new StackPanel();
        body.Children.Add(BuildHeader(row.CaparNumber));
        body.Children.Add(BuildDetailsBox(row));
        body.Children.Add(BuildSection("Root Cause", row.RootCause));
        body.Children.Add(BuildSection("Corrective Action", row.CorrectiveAction));
        body.Children.Add(BuildSection("Preventive Action", row.PreventiveAction));
        body.Children.Add(BuildSection("Remarks", row.Remarks));

        var footer = BuildFooter();
        DockPanel.SetDock(footer, Dock.Bottom);

        var dock = new DockPanel { ClipToBounds = true };
        dock.Children.Add(footer);
        dock.Children.Add(body);

        return new Border
        {
            Width = width,
            Height = height,
            Padding = new Thickness(padding),
            Background = Brushes.White,
            Child = dock
        };
    }

    /// <summary>Brand line, centered report title, and the accent rule underneath.</summary>
    private static StackPanel BuildHeader(string caparNumber)
    {
        var brand = new StackPanel();
        brand.Children.Add(new TextBlock { Text = "QA HUB", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brush("#1B2430") });
        brand.Children.Add(new TextBlock { Text = "Quality & Testing Center", FontSize = 9, Foreground = Brush("#6B7280") });

        var reportNo = new TextBlock
        {
            Text = $"Report No. {caparNumber}",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#111827"),
            VerticalAlignment = VerticalAlignment.Top
        };

        var topRow = new DockPanel();
        DockPanel.SetDock(reportNo, Dock.Right);
        DockPanel.SetDock(brand, Dock.Left);
        topRow.Children.Add(reportNo);
        topRow.Children.Add(brand);

        var title = new TextBlock
        {
            Text = "Corrective & Preventive Action Report",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#111827"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 2)
        };

        var rule = new Border
        {
            Height = 2,
            Background = Brush("#2F81F7"),
            CornerRadius = new CornerRadius(1),
            Margin = new Thickness(0, 10, 0, 0)
        };

        var header = new StackPanel();
        header.Children.Add(topRow);
        header.Children.Add(title);
        header.Children.Add(rule);
        return header;
    }

    /// <summary>The plain bordered box: title across the top, then a two-column label/value grid.</summary>
    private static Border BuildDetailsBox(CaparReportRow row)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var i = 0; i < 4; i++)
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddField(grid, 0, 0, "Status", row.Status.ToString());
        AddField(grid, 0, 1, "Source", row.Source.ToString());
        AddField(grid, 1, 0, "Severity", row.Severity.ToString());
        AddField(grid, 1, 1, "Project", row.Project);
        AddField(grid, 2, 0, "Owner", row.Owner);
        AddField(grid, 2, 1, "Opened", row.OpenedDate.ToString("MMM d, yyyy"));
        AddField(grid, 3, 0, "Due", row.DueDate?.ToString("MMM d, yyyy") ?? "—");
        AddField(grid, 3, 1, "Closed", row.ClosedDate?.ToString("MMM d, yyyy") ?? "—");

        var box = new StackPanel();
        box.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(row.Title) ? "Untitled" : row.Title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#111827"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 14)
        });
        box.Children.Add(grid);

        return new Border
        {
            BorderBrush = Brush("#D1D5DB"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(16),
            Child = box,
            Margin = new Thickness(0, 14, 0, 0)
        };
    }

    private static void AddField(Grid grid, int row, int col, string label, string value)
    {
        var panel = new StackPanel { Margin = new Thickness(col == 0 ? 0 : 14, 0, 0, 10) };
        panel.Children.Add(new TextBlock
        {
            Text = label.ToUpperInvariant(),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush("#9CA3AF")
        });
        panel.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "—" : value,
            FontSize = 12,
            Foreground = Brush("#111827"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0)
        });
        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, col);
        grid.Children.Add(panel);
    }

    /// <summary>Accent-ticked heading plus the body text.</summary>
    private static StackPanel BuildSection(string heading, string body)
    {
        var tick = new Border
        {
            Width = 3,
            Height = 14,
            Background = Brush("#2F81F7"),
            CornerRadius = new CornerRadius(1),
            VerticalAlignment = VerticalAlignment.Center
        };

        var headingRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        headingRow.Children.Add(tick);
        headingRow.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#111827"),
            Margin = new Thickness(6, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        var section = new StackPanel();
        section.Children.Add(headingRow);
        section.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(body) ? "None recorded." : body,
            FontSize = 11,
            Foreground = Brush("#374151"),
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 17,
            Margin = new Thickness(9, 4, 0, 0)
        });
        return section;
    }

    /// <summary>Thin rule and a centered "generated by" line pinned to the page bottom.</summary>
    private static StackPanel BuildFooter()
    {
        var rule = new Border
        {
            Height = 1,
            Background = Brush("#E5E7EB"),
            Margin = new Thickness(0, 18, 0, 6)
        };
        var line = new TextBlock
        {
            Text = $"Generated by QA Hub • {DateTime.Now:MMM d, yyyy h:mm tt}",
            FontSize = 9,
            Foreground = Brush("#9CA3AF"),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var footer = new StackPanel();
        footer.Children.Add(rule);
        footer.Children.Add(line);
        return footer;
    }

    private static SolidColorBrush Brush(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex));
}