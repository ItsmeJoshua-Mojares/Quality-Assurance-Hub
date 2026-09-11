using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using QAHub.Services;

namespace QAHub.ViewModels;

/// <summary>
/// Presentation wrapper around a single log line, formatted for the
/// dark terminal panel (timestamp / direction / message + color).
/// </summary>
public class SerialLogEntryViewModel
{
    public string TimestampLabel { get; }
    public string Direction { get; }
    public string Message { get; }
    public Brush DirectionBrush { get; }

    public SerialLogEntryViewModel(SerialLogEntry entry)
    {
        TimestampLabel = entry.Timestamp.ToString("HH:mm:ss.fff");
        Direction = entry.Direction;
        Message = entry.Message;

        DirectionBrush = entry.Direction switch
        {
            "TX" => new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8)),    // blue
            "RX" => new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80)),    // green
            "ERROR" => new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)), // red
            _ => new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8)),       // gray
        };
    }
}

public class SerialTerminalViewModel : ObservableObject, IDisposable
{
    private readonly SerialComService _service = new();

    private string? _selectedPort;
    private int _selectedBaudRate = 9600;
    private Parity _selectedParity = Parity.None;
    private int _selectedDataBits = 8;
    private StopBits _selectedStopBits = StopBits.One;
    private Handshake _selectedHandshake = Handshake.None;
    private bool _showAsHex;
    private bool _isConnected;
    private string _outgoingMessage = string.Empty;
    private int _bytesSent;
    private int _bytesReceived;
    private int _errorCount;

    public SerialTerminalViewModel()
    {
        RefreshPorts();

        _service.DataReceived += (s, e) =>
        {
            BytesReceived += e.RawData.Length;
            RefreshLog();
        };
        _service.ErrorOccurred += (s, e) =>
        {
            ErrorCount++;
            RefreshLog();
        };
        _service.PortOpened += (s, e) => IsConnected = true;
        _service.PortClosed += (s, e) => IsConnected = false;

        ConnectCommand = new RelayCommand(_ => Connect(), _ => IsNotConnected && !string.IsNullOrEmpty(SelectedPort));
        DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);
        SendCommand = new RelayCommand(_ => Send(), _ => IsConnected && !string.IsNullOrWhiteSpace(OutgoingMessage));
        RefreshPortsCommand = new RelayCommand(_ => RefreshPorts(), _ => IsNotConnected);
        ClearLogCommand = new RelayCommand(_ =>
        {
            _service.ClearLog();
            RefreshLog();
            BytesSent = 0;
            BytesReceived = 0;
            ErrorCount = 0;
        });
        ExportLogCommand = new RelayCommand(_ => ExportLog());
    }

    // ---- Options for the settings ComboBoxes ----
    public ObservableCollection<string> AvailablePorts { get; } = new();
    public int[] BaudRates { get; } = { 1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
    public Parity[] ParityOptions { get; } = (Parity[])Enum.GetValues(typeof(Parity));
    public int[] DataBitsOptions { get; } = { 5, 6, 7, 8 };
    public StopBits[] StopBitsOptions { get; } = { StopBits.One, StopBits.OnePointFive, StopBits.Two };
    public Handshake[] HandshakeOptions { get; } = (Handshake[])Enum.GetValues(typeof(Handshake));

    public ObservableCollection<SerialLogEntryViewModel> LogEntries { get; } = new();

    // ---- Selected settings ----
    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int SelectedBaudRate { get => _selectedBaudRate; set => SetProperty(ref _selectedBaudRate, value); }
    public Parity SelectedParity { get => _selectedParity; set => SetProperty(ref _selectedParity, value); }
    public int SelectedDataBits { get => _selectedDataBits; set => SetProperty(ref _selectedDataBits, value); }
    public StopBits SelectedStopBits { get => _selectedStopBits; set => SetProperty(ref _selectedStopBits, value); }
    public Handshake SelectedHandshake { get => _selectedHandshake; set => SetProperty(ref _selectedHandshake, value); }

    public bool ShowAsHex
    {
        get => _showAsHex;
        set
        {
            if (SetProperty(ref _showAsHex, value))
            {
                _service.DecodeAsText = !value;
            }
        }
    }

    public string OutgoingMessage
    {
        get => _outgoingMessage;
        set
        {
            if (SetProperty(ref _outgoingMessage, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    // ---- Status ----
    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(IsNotConnected));
                OnPropertyChanged(nameof(ConnectionStatusLabel));
                OnPropertyChanged(nameof(ConnectionStatusBrush));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsNotConnected => !IsConnected;

    public string ConnectionStatusLabel => IsConnected ? "Connected" : "Disconnected";

    public Brush ConnectionStatusBrush => IsConnected
        ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
        : new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80));

    public int BytesSent { get => _bytesSent; private set => SetProperty(ref _bytesSent, value); }
    public int BytesReceived { get => _bytesReceived; private set => SetProperty(ref _bytesReceived, value); }
    public int ErrorCount { get => _errorCount; private set => SetProperty(ref _errorCount, value); }

    // ---- Commands ----
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }
    public ICommand SendCommand { get; }
    public ICommand RefreshPortsCommand { get; }
    public ICommand ClearLogCommand { get; }
    public ICommand ExportLogCommand { get; }

    private void RefreshPorts()
    {
        var current = SelectedPort;
        AvailablePorts.Clear();
        foreach (var port in SerialComService.GetAvailablePorts().OrderBy(p => p))
        {
            AvailablePorts.Add(port);
        }

        SelectedPort = AvailablePorts.Contains(current!) ? current : AvailablePorts.FirstOrDefault();
    }

    private void Connect()
    {
        try
        {
            _service.PortName = SelectedPort!;
            _service.BaudRate = SelectedBaudRate;
            _service.Parity = SelectedParity;
            _service.DataBits = SelectedDataBits;
            _service.StopBits = SelectedStopBits;
            _service.Handshake = SelectedHandshake;
            _service.Open();
            RefreshLog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open {SelectedPort}:\n{ex.Message}", "Connection Failed",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Disconnect()
    {
        _service.Close();
        RefreshLog();
    }

    private void Send()
    {
        try
        {
            _service.Send(OutgoingMessage);
            BytesSent += System.Text.Encoding.ASCII.GetByteCount(OutgoingMessage);
            OutgoingMessage = string.Empty;
            RefreshLog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Send failed:\n{ex.Message}", "Send Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportLog()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"serial-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt"
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, _service.ExportLog());
        }
    }

    private void RefreshLog()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            LogEntries.Clear();
            foreach (var entry in _service.GetLog())
            {
                LogEntries.Add(new SerialLogEntryViewModel(entry));
            }
        });
    }

    public void Dispose()
    {
        _service.Dispose();
    }
}