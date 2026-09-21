namespace QAHub.Models;

public static class StandardCategoryExtensions
{
    /// <summary>Returns a human-friendly display label for a StandardCategory.</summary>
    public static string GetDisplayName(this StandardCategory category)
    {
        return category switch
        {
            StandardCategory.Ansi => "ANSI",
            StandardCategory.Iec => "IEC",
            StandardCategory.Iso => "ISO",
            StandardCategory.CompanySpec => "Company Spec",
            _ => "Other"
        };
    }
}