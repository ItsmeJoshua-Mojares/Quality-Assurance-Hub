namespace QAHub.Models;

public enum Priority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TestStatus
{
    Draft,
    Ready,
    Passed,
    Failed,
    Blocked
}

public enum Severity
{
    Minor,
    Normal,
    Major,
    Critical
}

public enum BugStatus
{
    New,
    Confirmed,
    InProgress,
    Fixed,
    Closed
}