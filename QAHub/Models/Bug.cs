namespace QAHub.Models;

public class Bug
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Severity { get; set; } = Severity.Normal;
    public BugStatus Status { get; set; } = BugStatus.New;
    public string Assignee { get; set; } = string.Empty;
    public DateTime OpenedDate { get; set; } = DateTime.Today;
}