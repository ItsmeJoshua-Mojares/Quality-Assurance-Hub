using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QAHub.Models;

/// <summary>Where a CAPAR request came from.</summary>
public enum CaparSource
{
    Internal,
    Customer,
    Rma,
    Audit
}

/// <summary>Lifecycle of a corrective &amp; preventive action request.</summary>
public enum CaparStatus
{
    Open,
    InProgress,
    Implemented,
    Verified,
    Closed
}

/// <summary>
/// A Corrective &amp; Preventive Action Request. Captures the non-conformance
/// source, root cause, and the corrective/preventive actions taken, plus the
/// owner and who-tracked dates. Implements INotifyPropertyChanged so it can be
/// edited in place.
/// </summary>
public class Capar : INotifyPropertyChanged
{
    private string _caparNumber = string.Empty;
    private string _title = string.Empty;
    private CaparSource _source = CaparSource.Internal;
    private string _project = string.Empty;
    private Severity _severity = Severity.Normal;
    private string _rootCause = string.Empty;
    private string _correctiveAction = string.Empty;
    private string _preventiveAction = string.Empty;
    private string _owner = string.Empty;
    private CaparStatus _status = CaparStatus.Open;
    private DateTime _openedDate = DateTime.Today;
    private DateTime? _dueDate;
    private DateTime? _closedDate;
    private string? _remarks;
    private DateTime _updatedAt = DateTime.Now;

    public int Id { get; set; }

    /// <summary>Human-friendly reference, e.g. "CAPAR-0001".</summary>
    public string CaparNumber
    {
        get => _caparNumber;
        set { _caparNumber = value; OnPropertyChanged(); Touch(); }
    }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); Touch(); }
    }

    public CaparSource Source
    {
        get => _source;
        set { _source = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Project/product the request is scoped to (optional).</summary>
    public string Project
    {
        get => _project;
        set { _project = value; OnPropertyChanged(); Touch(); }
    }

    public Severity Severity
    {
        get => _severity;
        set { _severity = value; OnPropertyChanged(); Touch(); }
    }

    public string RootCause
    {
        get => _rootCause;
        set { _rootCause = value; OnPropertyChanged(); Touch(); }
    }

    public string CorrectiveAction
    {
        get => _correctiveAction;
        set { _correctiveAction = value; OnPropertyChanged(); Touch(); }
    }

    public string PreventiveAction
    {
        get => _preventiveAction;
        set { _preventiveAction = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Person responsible for driving the action to closure.</summary>
    public string Owner
    {
        get => _owner;
        set { _owner = value; OnPropertyChanged(); Touch(); }
    }

    public CaparStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); Touch(); }
    }

    public DateTime OpenedDate
    {
        get => _openedDate;
        set { _openedDate = value; OnPropertyChanged(); Touch(); }
    }

    public DateTime? DueDate
    {
        get => _dueDate;
        set { _dueDate = value; OnPropertyChanged(); Touch(); }
    }

    public DateTime? ClosedDate
    {
        get => _closedDate;
        set { _closedDate = value; OnPropertyChanged(); Touch(); }
    }

    public string? Remarks
    {
        get => _remarks;
        set { _remarks = value; OnPropertyChanged(); Touch(); }
    }

    [JsonInclude]
    public DateTime UpdatedAt
    {
        get => _updatedAt;
        private set { _updatedAt = value; OnPropertyChanged(); }
    }

    private void Touch() => UpdatedAt = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}