using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace QAHub.Models;

public enum ProjectStatus
{
    Active,
    Archived
}

/// <summary>
/// Top-level container for scoping test cases, runs, defects, and requirements
/// to a specific product/app. Implements INotifyPropertyChanged directly so it
/// can be edited in place.
/// </summary>
public class Project : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _owner;
    private ProjectStatus _status = ProjectStatus.Active;
    private DateTime _updatedAt = DateTime.Now;

    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); Touch(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); Touch(); }
    }

    /// <summary>Team or person responsible for this project (optional).</summary>
    public string? Owner
    {
        get => _owner;
        set { _owner = value; OnPropertyChanged(); Touch(); }
    }

    public ProjectStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); Touch(); }
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