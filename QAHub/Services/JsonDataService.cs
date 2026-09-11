using System.IO;
using System.Text.Json;
using QAHub.Models;

namespace QAHub.Services;

public class JsonDataService : IDataService
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _dataDirectory;
    private readonly string _testCasesPath;
    private readonly string _bugsPath;

    public JsonDataService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(_dataDirectory);

        _testCasesPath = Path.Combine(_dataDirectory, "testcases.json");
        _bugsPath = Path.Combine(_dataDirectory, "bugs.json");
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

    public void SaveTestCases(IEnumerable<TestCase> testCases)
        => File.WriteAllText(_testCasesPath, JsonSerializer.Serialize(testCases, Options));

    public void SaveBugs(IEnumerable<Bug> bugs)
        => File.WriteAllText(_bugsPath, JsonSerializer.Serialize(bugs, Options));
}