using System;
using System.Collections.Generic;
using QAHub.Models;

namespace QAHub.Services;

/// <summary>Seeded reference data shown the first time the app runs
/// (and any time a data file is missing). Keeps sample content for
/// test cases, bugs, and shipments separate from the JSON persistence
/// layer. Projects intentionally start empty and only persist the
/// projects the user creates.</summary>
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
            Title = "Search with empty input",
            Description = "Verify an empty search shows all results without error.",
            Area = "Search",
            Priority = Priority.Medium,
            Status = TestStatus.Ready
        },
        new TestCase
        {
            Id = 8,
            Title = "Checkout validation - required fields",
            Description = "Verify required fields are validated on the checkout screen.",
            Area = "Checkout",
            Priority = Priority.High,
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

    public static List<Shipment> CreateShipments() => new()
    {
        new Shipment
        {
            Id = 1,
            ProjectId = 1,
            ProjectName = "Atlas IoT Gateway",
            ShipmentDate = DateTime.Today.AddDays(-12),
            SerialNumber = "GW-200-0001",
            Model = "GW-200",
            FirmwareVersion = "1.4.0",
            QaStatus = ShipmentQaStatus.Ready,
            Location = "Warehouse A"
        },
        new Shipment
        {
            Id = 2,
            ProjectId = 2,
            ProjectName = "Aurora Sensor Hub",
            ShipmentDate = DateTime.Today.AddDays(-4),
            SerialNumber = "SH-50-0012",
            Model = "SH-50",
            FirmwareVersion = "0.9.1",
            QaStatus = ShipmentQaStatus.Passed,
            Location = "In transit to Cebu"
        },
        new Shipment
        {
            Id = 3,
            ProjectId = 1,
            ProjectName = "Atlas IoT Gateway",
            ShipmentDate = DateTime.Today.AddDays(-1),
            SerialNumber = "GW-200-0002",
            Model = "GW-200",
            FirmwareVersion = "1.5.0",
            QaStatus = ShipmentQaStatus.InProgress,
            Location = "QA bench"
        }
    };
}
