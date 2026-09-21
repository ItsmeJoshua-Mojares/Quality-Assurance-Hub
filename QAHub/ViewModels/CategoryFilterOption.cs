using QAHub.Models;

namespace QAHub.ViewModels;

/// <summary>
/// A selectable category-filter option. A null Value represents "All categories",
/// avoiding the WPF problem of a null selection box showing blank text.
/// </summary>
public sealed class CategoryFilterOption
{
    public CategoryFilterOption(string label, StandardCategory? value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public StandardCategory? Value { get; }
}