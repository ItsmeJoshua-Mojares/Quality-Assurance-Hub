using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QAHub.Models;

public enum RequirementCoverageStatus
{
    Untested,
    PartiallyTested,
    FullyTested
}

/// <summary>
/// A single requirement, traced to the test cases that cover it.
/// Implements INotifyPropertyChanged directly so it can be edited in place.
/// </summary>
public class Requirement : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string? _source;
    private RequirementCoverageStatus _coverageStatus = RequirementCoverageStatus.Untested;
    private string? _linkedTestCaseNames;
    private List<int> _linkedTestCaseIds = new();
    private DateTime _updatedAt = DateTime.Now;

    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); Touch(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Where the requirement came from — a spec doc, ticket, or stakeholder.</summary>
    public string? Source
    {
        get => _source;
        set { _source = value; OnPropertyChanged(); Touch(); }
    }

    public RequirementCoverageStatus CoverageStatus
    {
        get => _coverageStatus;
        set { _coverageStatus = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>
    /// Legacy free-text field from before real Test Case linking existed.
    /// Kept (rather than removed) so existing requirements.json data isn't
    /// lost on load — no longer written to by the UI, but still readable.
    /// </summary>
    [Obsolete("Superseded by LinkedTestCaseIds. Kept only for backward compatibility with older saved data.")]
    public string? LinkedTestCaseNames
    {
        get => _linkedTestCaseNames;
        set { _linkedTestCaseNames = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Real link to TestCase.Id — the actual test cases covering this requirement.</summary>
    public List<int> LinkedTestCaseIds
    {
        get => _linkedTestCaseIds;
        set { _linkedTestCaseIds = value ?? new List<int>(); OnPropertyChanged(); Touch(); }
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