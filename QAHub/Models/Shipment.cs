using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QAHub.Models;

/// <summary>QA gate on the firmware/build in the shipment.</summary>
public enum ShipmentQaStatus
{
    Pending,
    InProgress,
    Ready,
    Passed,
    Blocked
}

/// <summary>
/// A product shipment tied to a Project. Tracks the QA status of the firmware
/// build plus the physical shipment (date, serial number, model, location, and
/// remarks). Implements INotifyPropertyChanged so it can be edited in place.
/// </summary>
public class Shipment : INotifyPropertyChanged
{
    private string _projectName = string.Empty;
    private DateTime _shipmentDate = DateTime.Today;
    private string _serialNumber = string.Empty;
    private string _model = string.Empty;
    private string _firmwareVersion = string.Empty;
    private ShipmentQaStatus _qaStatus = ShipmentQaStatus.Pending;
    private string? _location;
    private string? _remarks;
    private string? _masterBox;
    private string? _topEnclosureSn;
    private string? _imei;
    private DateTime _updatedAt = DateTime.Now;

    public int Id { get; set; }

    /// <summary>Id of the Project this shipment belongs to.</summary>
    public int ProjectId { get; set; }

    /// <summary>Denormalized project name used for display and filtering.</summary>
    public string ProjectName
    {
        get => _projectName;
        set { _projectName = value; OnPropertyChanged(); Touch(); }
    }

    public DateTime ShipmentDate
    {
        get => _shipmentDate;
        set { _shipmentDate = value; OnPropertyChanged(); Touch(); }
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

    public string FirmwareVersion
    {
        get => _firmwareVersion;
        set { _firmwareVersion = value; OnPropertyChanged(); Touch(); }
    }

    public ShipmentQaStatus QaStatus
    {
        get => _qaStatus;
        set { _qaStatus = value; OnPropertyChanged(); Touch(); }
    }

    public string? Location
    {
        get => _location;
        set { _location = value; OnPropertyChanged(); Touch(); }
    }

    public string? Remarks
    {
        get => _remarks;
        set { _remarks = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Packaged bundle (Master Box serial) this unit ships in, e.g. "MB260331B0001".</summary>
    public string? MasterBox
    {
        get => _masterBox;
        set { _masterBox = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Top enclosure serial number, e.g. "WE261005B0054".</summary>
    public string? TopEnclosureSn
    {
        get => _topEnclosureSn;
        set { _topEnclosureSn = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>IMEI of the unit.</summary>
    public string? Imei
    {
        get => _imei;
        set { _imei = value; OnPropertyChanged(); Touch(); }
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