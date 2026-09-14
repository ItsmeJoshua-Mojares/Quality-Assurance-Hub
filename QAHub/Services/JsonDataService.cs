using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using QAHub.Models;

namespace QAHub.Services;

public class JsonDataService : IDataService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _dataDirectory;
    private readonly string _testCasesPath;
    private readonly string _bugsPath;
    private readonly string _projectsPath;
    private readonly string _shipmentsPath;

    public JsonDataService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? ResolveDataDirectory();
        Directory.CreateDirectory(_dataDirectory);

        _testCasesPath = Path.Combine(_dataDirectory, "testcases.json");
        _bugsPath = Path.Combine(_dataDirectory, "bugs.json");
        _projectsPath = Path.Combine(_dataDirectory, "projects.json");
        _shipmentsPath = Path.Combine(_dataDirectory, "shipments.json");
    }

    /// <summary>
    /// Prefer a solution-level "data" folder so all app data lives next to the
    /// source in one place. Walks up from the executable until it finds the
    /// folder containing the solution file, then uses its "data" subfolder.
    /// Falls back to the executable's own "Data" folder (e.g. published builds).
    /// </summary>
    private static string ResolveDataDirectory()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return Path.Combine(dir.FullName, "data");
            dir = dir.Parent;
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
    }

    public List<TestCase> LoadTestCases()
    {
        if (!File.Exists(_testCasesPath)) return new List<TestCase>();

        return JsonSerializer.Deserialize<List<TestCase>>(File.ReadAllText(_testCasesPath), Options)
            ?? new List<TestCase>();
    }

    public List<Bug> LoadBugs()
    {
        if (!File.Exists(_bugsPath)) return new List<Bug>();

        return JsonSerializer.Deserialize<List<Bug>>(File.ReadAllText(_bugsPath), Options)
            ?? new List<Bug>();
    }

    public List<Project> LoadProjects()
    {
        if (!File.Exists(_projectsPath)) return new List<Project>();

        return JsonSerializer.Deserialize<List<Project>>(File.ReadAllText(_projectsPath), Options)
            ?? new List<Project>();
    }

    public List<Shipment> LoadShipments()
    {
        if (!File.Exists(_shipmentsPath)) return new List<Shipment>();

        return JsonSerializer.Deserialize<List<Shipment>>(File.ReadAllText(_shipmentsPath), Options)
            ?? new List<Shipment>();
    }

    public void SaveTestCases(IEnumerable<TestCase> testCases)
        => File.WriteAllText(_testCasesPath, JsonSerializer.Serialize(testCases, Options));

    public void SaveBugs(IEnumerable<Bug> bugs)
        => File.WriteAllText(_bugsPath, JsonSerializer.Serialize(bugs, Options));

    public void SaveProjects(IEnumerable<Project> projects)
        => File.WriteAllText(_projectsPath, JsonSerializer.Serialize(projects, Options));

    public void SaveShipments(IEnumerable<Shipment> shipments)
        => File.WriteAllText(_shipmentsPath, JsonSerializer.Serialize(shipments, Options));
}
