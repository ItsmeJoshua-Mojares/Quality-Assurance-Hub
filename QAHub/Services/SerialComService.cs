using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace QAHub.Services
{
    public class SerialDataReceivedEventArgs : EventArgs
    {
        public byte[] RawData { get; }
        public string Text { get; }
        public DateTime Timestamp { get; }

        public SerialDataReceivedEventArgs(byte[] rawData, string text)
        {
            RawData = rawData;
            Text = text;
            Timestamp = DateTime.Now;
        }
    }

    public class SerialErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }
        public DateTime Timestamp { get; }

        public SerialErrorEventArgs(string message, Exception exception = null)
        {
            Message = message;
            Exception = exception;
            Timestamp = DateTime.Now;
        }
    }

    public class SerialLogEntry
    {
        public DateTime Timestamp { get; }
        public string Direction { get; }
        public string Message { get; }

        public SerialLogEntry(string direction, string message)
        {
            Timestamp = DateTime.Now;
            Direction = direction;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Direction,-5} {Message}";
        }
    }

    public class SerialComService : IDisposable
    {
        private SerialPort _serialPort;
        private readonly object _syncLock = new object();
        private readonly List<SerialLogEntry> _log = new List<SerialLogEntry>();
        private bool _disposed;

        public event EventHandler<SerialDataReceivedEventArgs> DataReceived;
        public event EventHandler<SerialErrorEventArgs> ErrorOccurred;
        public event EventHandler PortOpened;
        public event EventHandler PortClosed;

        public string PortName { get; set; } = "COM1";
        public int BaudRate { get; set; } = 9600;
        public Parity Parity { get; set; } = Parity.None;
        public int DataBits { get; set; } = 8;
        public StopBits StopBits { get; set; } = StopBits.One;
        public Handshake Handshake { get; set; } = Handshake.None;
        public int ReadTimeoutMs { get; set; } = 1000;
        public int WriteTimeoutMs { get; set; } = 1000;

        public bool DecodeAsText { get; set; } = true;
        public Encoding TextEncoding { get; set; } = Encoding.ASCII;

        public bool IsOpen => _serialPort != null && _serialPort.IsOpen;

        public static string[] GetAvailablePorts()
        {
            return SerialPort.GetPortNames();
        }

        public void Open()
        {
            lock (_syncLock)
            {
                if (IsOpen)
                {
                    Log("INFO", $"Open() called but port {PortName} is already open.");
                    return;
                }

                try
                {
                    _serialPort = new SerialPort(PortName, BaudRate, Parity, DataBits, StopBits)
                    {
                        Handshake = Handshake,
                        ReadTimeout = ReadTimeoutMs,
                        WriteTimeout = WriteTimeoutMs
                    };

                    _serialPort.DataReceived += OnSerialPortDataReceived;
                    _serialPort.ErrorReceived += OnSerialPortErrorReceived;

                    _serialPort.Open();

                    Log("INFO", $"Opened {PortName} @ {BaudRate} baud, {DataBits}{ParityToChar(Parity)}{StopBitsToString(StopBits)}, handshake={Handshake}.");
                    PortOpened?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Log("ERROR", $"Failed to open {PortName}: {ex.Message}");
                    RaiseError($"Failed to open {PortName}: {ex.Message}", ex);
                    throw;
                }
            }
        }

        public void Close()
        {
            lock (_syncLock)
            {
                if (_serialPort == null)
                {
                    return;
                }

                try
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                        Log("INFO", $"Closed {PortName}.");
                        PortClosed?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Log("ERROR", $"Error closing {PortName}: {ex.Message}");
                    RaiseError($"Error closing {PortName}: {ex.Message}", ex);
                }
                finally
                {
                    _serialPort.DataReceived -= OnSerialPortDataReceived;
                    _serialPort.ErrorReceived -= OnSerialPortErrorReceived;
                    _serialPort.Dispose();
                    _serialPort = null;
                }
            }
        }

        /// <summary>
        /// Sends a text string (encoded using TextEncoding).
        /// </summary>
        public void Send(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            SendBytes(TextEncoding.GetBytes(data));
        }

        /// <summary>
        /// Sends a text string followed by a line terminator (e.g. "\r\n").
        /// </summary>
        public void SendLine(string data, string terminator = "\r\n")
        {
            Send(data + terminator);
        }

        /// <summary>
        /// Sends raw bytes over the port.
        /// </summary>
        public void SendBytes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            if (!IsOpen)
            {
                var msg = "SendBytes() called but port is not open.";
                Log("ERROR", msg);
                RaiseError(msg);
                throw new InvalidOperationException(msg);
            }

            try
            {
                _serialPort.Write(data, 0, data.Length);
                Log("TX", DecodeAsText ? TextEncoding.GetString(data) : BytesToHex(data));
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Send failed: {ex.Message}");
                RaiseError($"Send failed: {ex.Message}", ex);
                throw;
            }
        }

        private void OnSerialPortDataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            try
            {
                if (_serialPort == null || !_serialPort.IsOpen) return;

                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead <= 0) return;

                byte[] buffer = new byte[bytesToRead];
                int read = _serialPort.Read(buffer, 0, bytesToRead);
                if (read < bytesToRead)
                {
                    Array.Resize(ref buffer, read);
                }

                string text = DecodeAsText ? TextEncoding.GetString(buffer) : BytesToHex(buffer);
                Log("RX", text);

                DataReceived?.Invoke(this, new SerialDataReceivedEventArgs(buffer, text));
            }
            catch (Exception ex)
            {
                Log("ERROR", $"Receive failed: {ex.Message}");
                RaiseError($"Receive failed: {ex.Message}", ex);
            }
        }

        private void OnSerialPortErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            var msg = $"Serial error: {e.EventType}";
            Log("ERROR", msg);
            RaiseError(msg);
        }

        private void RaiseError(string message, Exception ex = null)
        {
            ErrorOccurred?.Invoke(this, new SerialErrorEventArgs(message, ex));
        }

        private void Log(string direction, string message)
        {
            var entry = new SerialLogEntry(direction, message);
            lock (_syncLock)
            {
                _log.Add(entry);
            }
        }

        public IReadOnlyList<SerialLogEntry> GetLog()
        {
            lock (_syncLock)
            {
                return new List<SerialLogEntry>(_log);
            }
        }

        public void ClearLog()
        {
            lock (_syncLock)
            {
                _log.Clear();
            }
        }

        public string ExportLog()
        {
            var sb = new StringBuilder();
            lock (_syncLock)
            {
                foreach (var entry in _log)
                {
                    sb.AppendLine(entry.ToString());
                }
            }
            return sb.ToString();
        }

        private static string BytesToHex(byte[] data)
        {
            var sb = new StringBuilder(data.Length * 3);
            foreach (var b in data)
            {
                sb.Append(b.ToString("X2")).Append(' ');
            }
            return sb.ToString().TrimEnd();
        }

        private static char ParityToChar(Parity parity)
        {
            switch (parity)
            {
                case Parity.None: return 'N';
                case Parity.Odd: return 'O';
                case Parity.Even: return 'E';
                case Parity.Mark: return 'M';
                case Parity.Space: return 'S';
                default: return '?';
            }
        }

        private static string StopBitsToString(StopBits stopBits)
        {
            switch (stopBits)
            {
                case StopBits.One: return "1";
                case StopBits.OnePointFive: return "1.5";
                case StopBits.Two: return "2";
                default: return "?";
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Close();
            }

            _disposed = true;
        }
    }
}