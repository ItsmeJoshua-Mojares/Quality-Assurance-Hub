using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QAHub.Models;

/// <summary>Lifecycle of a returned unit as it moves through RMA disposition.</summary>
public enum RmaStatus
{
    Received,
    Diagnosing,
    Repairing,
    ReTesting,
    ReShipped,
    Closed
}

/// <summary>
/// A Return Merchandise Authorization record tracking a unit that was sent
/// back by a customer. Tracks the disposition flow (received, diagnosing,
/// repairing, retesting, reshipped, closed) plus the reason it came back.
/// Implements INotifyPropertyChanged so it can be edited in place.
/// </summary>
public class Rma : INotifyPropertyChanged
{
    private string _rmaNumber = string.Empty;
    private string _serialNumber = string.Empty;
    private string _model = string.Empty;
    private string _project = string.Empty;
    private string _customer = string.Empty;
    private DateTime _receivedDate = DateTime.Today;
    private string _reason = string.Empty;
    private RmaStatus _status = RmaStatus.Received;
    private string? _remarks;
    private DateTime _updatedAt = DateTime.Now;

    public int Id { get; set; }

    /// <summary>Human-friendly reference, e.g. "RMA-0001".</summary>
    public string RmaNumber
    {
        get => _rmaNumber;
        set { _rmaNumber = value; OnPropertyChanged(); Touch(); }
    }

    public string SerialNumber
    {
        get => _serialNumber;
        set { _serialNumber = value; OnPropertyChanged(); Touch(); }
    }

    public string Model
    {
        get => _model;
        set { _model = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Project/product the returned unit belongs to (optional).</summary>
    public string Project
    {
        get => _project;
        set { _project = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Customer who returned the unit.</summary>
    public string Customer
    {
        get => _customer;
        set { _customer = value; OnPropertyChanged(); Touch(); }
    }

    public DateTime ReceivedDate
    {
        get => _receivedDate;
        set { _receivedDate = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Why the unit was returned (defect / failure description).</summary>
    public string Reason
    {
        get => _reason;
        set { _reason = value; OnPropertyChanged(); Touch(); }
    }

    public RmaStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); Touch(); }
    }

    public string? Remarks
    {
        get => _remarks;
        set { _remarks = value; OnPropertyChanged(); Touch(); }
    }

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