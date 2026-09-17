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
    private readonly string _testRunsPath;
    private readonly string _requirementsPath;
    private readonly string _knowledgeBasePath;
    private readonly string _rmAsPath;
    private readonly string _caparsPath;
    private readonly string _standardDocumentsPath;
    private readonly string _documentsDirectory;

    public JsonDataService(string? dataDirectory = null)
    {
        _dataDirectory = dataDirectory ?? ResolveDataDirectory();
        Directory.CreateDirectory(_dataDirectory);

        _testCasesPath = Path.Combine(_dataDirectory, "testcases.json");
        _bugsPath = Path.Combine(_dataDirectory, "bugs.json");
        _projectsPath = Path.Combine(_dataDirectory, "projects.json");
        _shipmentsPath = Path.Combine(_dataDirectory, "shipments.json");
        _testRunsPath = Path.Combine(_dataDirectory, "testruns.json");
        _requirementsPath = Path.Combine(_dataDirectory, "requirements.json");
        _knowledgeBasePath = Path.Combine(_dataDirectory, "knowledgebase.json");
        _rmAsPath = Path.Combine(_dataDirectory, "rma.json");
        _caparsPath = Path.Combine(_dataDirectory, "capar.json");
        _standardDocumentsPath = Path.Combine(_dataDirectory, "documents.json");

        _documentsDirectory = Path.Combine(_dataDirectory, "documents");
        Directory.CreateDirectory(_documentsDirectory);
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

    public List<TestRun> LoadTestRuns()
    {
        if (!File.Exists(_testRunsPath)) return new List<TestRun>();

        return JsonSerializer.Deserialize<List<TestRun>>(File.ReadAllText(_testRunsPath), Options)
            ?? new List<TestRun>();
    }

    public List<Requirement> LoadRequirements()
    {
        if (!File.Exists(_requirementsPath)) return new List<Requirement>();

        return JsonSerializer.Deserialize<List<Requirement>>(File.ReadAllText(_requirementsPath), Options)
            ?? new List<Requirement>();
    }

    public List<KnowledgeArticle> LoadKnowledgeArticles()
    {
        if (!File.Exists(_knowledgeBasePath)) return new List<KnowledgeArticle>();

        return JsonSerializer.Deserialize<List<KnowledgeArticle>>(File.ReadAllText(_knowledgeBasePath), Options)
            ?? new List<KnowledgeArticle>();
    }

    public void SaveTestCases(IEnumerable<TestCase> testCases)
        => File.WriteAllText(_testCasesPath, JsonSerializer.Serialize(testCases, Options));

    public void SaveBugs(IEnumerable<Bug> bugs)
        => File.WriteAllText(_bugsPath, JsonSerializer.Serialize(bugs, Options));

    public void SaveProjects(IEnumerable<Project> projects)
        => File.WriteAllText(_projectsPath, JsonSerializer.Serialize(projects, Options));

    public void SaveShipments(IEnumerable<Shipment> shipments)
        => File.WriteAllText(_shipmentsPath, JsonSerializer.Serialize(shipments, Options));

    public void SaveTestRuns(IEnumerable<TestRun> testRuns)
        => File.WriteAllText(_testRunsPath, JsonSerializer.Serialize(testRuns, Options));

    public void SaveRequirements(IEnumerable<Requirement> requirements)
        => File.WriteAllText(_requirementsPath, JsonSerializer.Serialize(requirements, Options));

    public void SaveKnowledgeArticles(IEnumerable<KnowledgeArticle> articles)
        => File.WriteAllText(_knowledgeBasePath, JsonSerializer.Serialize(articles, Options));

    public List<Rma> LoadRmAs()
    {
        if (!File.Exists(_rmAsPath)) return new List<Rma>();

        return JsonSerializer.Deserialize<List<Rma>>(File.ReadAllText(_rmAsPath), Options)
            ?? new List<Rma>();
    }

    public List<Capar> LoadCapars()
    {
        if (!File.Exists(_caparsPath)) return new List<Capar>();

        return JsonSerializer.Deserialize<List<Capar>>(File.ReadAllText(_caparsPath), Options)
            ?? new List<Capar>();
    }

    public void SaveRmAs(IEnumerable<Rma> rmAs)
        => File.WriteAllText(_rmAsPath, JsonSerializer.Serialize(rmAs, Options));

    public void SaveCapars(IEnumerable<Capar> capars)
        => File.WriteAllText(_caparsPath, JsonSerializer.Serialize(capars, Options));

    public List<StandardDocument> LoadStandardDocuments()
    {
        if (!File.Exists(_standardDocumentsPath)) return new List<StandardDocument>();

        return JsonSerializer.Deserialize<List<StandardDocument>>(File.ReadAllText(_standardDocumentsPath), Options)
            ?? new List<StandardDocument>();
    }

    public void SaveStandardDocuments(IEnumerable<StandardDocument> documents)
        => File.WriteAllText(_standardDocumentsPath, JsonSerializer.Serialize(documents, Options));

    public StandardDocument StoreDocumentFile(string sourcePath, string fileTitle, string description, StandardCategory category)
    {
        var originalFileName = Path.GetFileName(sourcePath);
        var extension = Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant();
        var storedName = BuildUniqueStoredName(originalFileName);
        var storedPath = Path.Combine(_documentsDirectory, storedName);
        File.Copy(sourcePath, storedPath);

        return new StandardDocument
        {
            Id = 0,
            Title = fileTitle,
            Description = description,
            Category = category,
            FileName = originalFileName,
            StoredName = storedName,
            Extension = extension,
            SizeBytes = new FileInfo(storedPath).Length,
            UploadedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Derives the on-disk name from the original file name so the stored document
    /// keeps the user's chosen name. Sanitizes characters that are invalid in
    /// Windows paths and appends "(n)" before the extension when a name collision
    /// exists, so nothing is ever overwritten.
    /// </summary>
    private string BuildUniqueStoredName(string originalFileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(originalFileName
            .Select(c => invalid.Contains(c) ? '_' : c)
            .ToArray())
            .TrimEnd('.', ' ')
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
            sanitized = "document";

        // Guard against absurdly long names leaking past the Windows path limit.
        if (sanitized.Length > 180)
        {
            var name = Path.GetFileNameWithoutExtension(sanitized);
            var ext = Path.GetExtension(sanitized);
            var truncated = name.Length > 160 ? name.Substring(0, 160) : name;
            sanitized = truncated + ext;
        }

        var candidate = sanitized;
        var counter = 1;
        while (File.Exists(Path.Combine(_documentsDirectory, candidate)))
        {
            var name = Path.GetFileNameWithoutExtension(sanitized);
            var ext = Path.GetExtension(sanitized);
            candidate = $"{name} ({counter}){ext}";
            counter++;
        }

        return candidate;
    }

    public string? GetDocumentPath(StandardDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.StoredName)) return null;

        var path = Path.Combine(_documentsDirectory, document.StoredName);
        return File.Exists(path) ? path : null;
    }

    public void DeleteDocumentFile(StandardDocument document)
    {
        if (string.IsNullOrWhiteSpace(document.StoredName)) return;

        var path = Path.Combine(_documentsDirectory, document.StoredName);
        if (File.Exists(path)) File.Delete(path);
    }
}
