using QAHub.Models;

namespace QAHub.Services;

public static class SampleData
{
    public static List<TestCase> CreateTestCases() => new()
    {
        new TestCase
        {
            Id = 1,
            Title = "Login with valid credentials",
            Description = "Verify a user can sign in with valid credentials.",
            Area = "Auth",
            Priority = Priority.High,
            Status = TestStatus.Passed
        },
        new TestCase
        {
            Id = 2,
            Title = "Login with invalid credentials",
            Description = "Verify a clear error is shown for bad credentials.",
            Area = "Auth",
            Priority = Priority.High,
            Status = TestStatus.Passed
        },
        new TestCase
        {
            Id = 3,
            Title = "Password reset flow",
            Description = "Verify the reset email is sent and the password can be changed.",
            Area = "Auth",
            Priority = Priority.Medium,
            Status = TestStatus.Ready
        },
        new TestCase
        {
            Id = 4,
            Title = "Add item to cart",
            Description = "Verify an item can be added to the cart and quantity updates.",
            Area = "Cart",
            Priority = Priority.High,
            Status = TestStatus.Passed
        },
        new TestCase
        {
            Id = 5,
            Title = "Checkout validation",
            Description = "Verify required fields are validated on the checkout screen.",
            Area = "Checkout",
            Priority = Priority.High,
            Status = TestStatus.Failed
        },
        new TestCase
        {
            Id = 6,
            Title = "Search products",
            Description = "Verify search returns relevant results and handles empty input.",
            Area = "Search",
            Priority = Priority.Medium,
            Status = TestStatus.Draft
        },
        new TestCase
        {
            Id = 7,
            Title = "Dark mode toggle",
            Description = "Verify the theme switch applies instantly and persists.",
            Area = "Settings",
            Priority = Priority.Low,
            Status = TestStatus.Ready
        },
        new TestCase
        {
            Id = 8,
            Title = "Payment with card",
            Description = "Verify a card payment succeeds; blocked by sandbox outage.",
            Area = "Billing",
            Priority = Priority.Critical,
            Status = TestStatus.Blocked
        }
    };

    public static List<Bug> CreateBugs() => new()
    {
        new Bug
        {
            Id = 1,
            Title = "Cart total wrong after coupon",
            Description = "Discount is not applied to the subtotal.",
            Severity = Severity.Major,
            Status = BugStatus.InProgress,
            Assignee = "Jordan",
            OpenedDate = DateTime.Today.AddDays(-3)
        },
        new Bug
        {
            Id = 2,
            Title = "Crash on rapid checkout clicks",
            Description = "Double-clicking Pay causes an unhandled exception.",
            Severity = Severity.Critical,
            Status = BugStatus.Confirmed,
            Assignee = "Priya",
            OpenedDate = DateTime.Today.AddDays(-1)
        },
        new Bug
        {
            Id = 3,
            Title = "Search ignores accents",
            Description = "A query typed without accents returns no matches.",
            Severity = Severity.Minor,
            Status = BugStatus.New,
            Assignee = "Marcus",
            OpenedDate = DateTime.Today
        },
        new Bug
        {
            Id = 4,
            Title = "Session timeout message missing",
            Description = "The user is silently logged out with no feedback.",
            Severity = Severity.Normal,
            Status = BugStatus.Closed,
            Assignee = "Jordan",
            OpenedDate = DateTime.Today.AddDays(-6)
        }
    };
}