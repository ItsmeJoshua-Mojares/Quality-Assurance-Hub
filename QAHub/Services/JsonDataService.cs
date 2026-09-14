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
        _dataDirectory = dataDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        Directory.CreateDirectory(_dataDirectory);

        _testCasesPath = Path.Combine(_dataDirectory, "testcases.json");
        _bugsPath = Path.Combine(_dataDirectory, "bugs.json");
        _projectsPath = Path.Combine(_dataDirectory, "projects.json");
        _shipmentsPath = Path.Combine(_dataDirectory, "shipments.json");
    }

    public List<TestCase> LoadTestCases()
    {
        if (!File.Exists(_testCasesPath))
        {
            var seeds = SampleData.CreateTestCases();
            SaveTestCases(seeds);
            return seeds;
        }

        return JsonSerializer.Deserialize<List<TestCase>>(File.ReadAllText(_testCasesPath), Options)
            ?? new List<TestCase>();
    }

    public List<Bug> LoadBugs()
    {
        if (!File.Exists(_bugsPath))
        {
            var seeds = SampleData.CreateBugs();
            SaveBugs(seeds);
            return seeds;
        }

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
        if (!File.Exists(_shipmentsPath))
        {
            var seeds = SampleData.CreateShipments();
            SaveShipments(seeds);
            return seeds;
        }

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
