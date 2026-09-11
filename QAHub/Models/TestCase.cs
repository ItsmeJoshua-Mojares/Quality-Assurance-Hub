namespace QAHub.Models;

public class TestCase
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.Medium;
    public TestStatus Status { get; set; } = TestStatus.Draft;
}