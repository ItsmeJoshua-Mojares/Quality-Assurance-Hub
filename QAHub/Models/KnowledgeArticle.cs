using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QAHub.Models;

public enum KnowledgeCategory
{
    KnownIssue,
    EnvironmentSetup,
    HowTo,
    DomainNotes,
    Other
}

/// <summary>
/// A single knowledge base article — a known issue/workaround, setup guide,
/// how-to, or domain note. Implements INotifyPropertyChanged directly so it
/// can be edited in place (two-way bound TextBoxes) without a separate
/// edit-mode view model.
/// </summary>
public class KnowledgeArticle : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _content = string.Empty;
    private KnowledgeCategory _category = KnowledgeCategory.Other;
    private string? _tags;
    private DateTime _updatedAt = DateTime.Now;

    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); Touch(); }
    }

    public string Content
    {
        get => _content;
        set { _content = value; OnPropertyChanged(); Touch(); }
    }

    public KnowledgeCategory Category
    {
        get => _category;
        set { _category = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Comma-separated tags, e.g. "serial-com, staging, known-issue".</summary>
    public string? Tags
    {
        get => _tags;
        set { _tags = value; OnPropertyChanged(); Touch(); }
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
