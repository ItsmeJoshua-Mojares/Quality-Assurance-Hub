using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QAHub.Models;

public enum StandardCategory
{
    Ansi,
    Iec,
    Iso,
    CompanySpec,
    Other
}

/// <summary>
/// A registered standard/reference document (ANSI, IEC, ISO, ...) whose binary
/// file lives in the git-ignored data folder. Only lightweight metadata is kept
/// in documents.json; the file itself is copied into data/documents/.
/// </summary>
public class StandardDocument : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private StandardCategory _category = StandardCategory.Other;

    public int Id { get; set; }

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }

    public StandardCategory Category
    {
        get => _category;
        set { _category = value; OnPropertyChanged(); }
    }

    /// <summary>Original file name shown to the user, e.g. "IEC 61000-4-2.pdf".</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Sanitized name on disk (GUID + extension) to avoid collisions.</summary>
    public string StoredName { get; set; } = string.Empty;

    /// <summary>Lowercase file extension, e.g. "pdf".</summary>
    public string Extension { get; set; } = string.Empty;

    /// <summary>Size of the stored file in bytes.</summary>
    public long SizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.Now;

    [JsonIgnore]
    public string SizeDisplay => SizeBytes >= 1024 * 1024
        ? $"{SizeBytes / (1024.0 * 1024.0):0.#} MB"
        : $"{Math.Max(1, SizeBytes / 1024)} KB";

    [JsonIgnore]
    public bool IsPdf => string.Equals(Extension, "pdf", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string ExtensionDisplay => string.IsNullOrWhiteSpace(Extension) ? "FILE" : Extension.ToUpperInvariant();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}