using System.Collections.Generic;
using System.Linq;
using QAHub.Models;

namespace QAHub.Services;

public interface IDataService
{
    List<TestCase> LoadTestCases();
    List<Bug> LoadBugs();
    List<Project> LoadProjects();
    List<Shipment> LoadShipments();
    List<TestRun> LoadTestRuns();
    List<Requirement> LoadRequirements();
    List<KnowledgeArticle> LoadKnowledgeArticles();

    List<Rma> LoadRmAs();
    List<Capar> LoadCapars();
    List<StandardDocument> LoadStandardDocuments();
    void SaveTestCases(IEnumerable<TestCase> testCases);
    void SaveBugs(IEnumerable<Bug> bugs);
    void SaveProjects(IEnumerable<Project> projects);
    void SaveShipments(IEnumerable<Shipment> shipments);
    void SaveTestRuns(IEnumerable<TestRun> testRuns);
    void SaveRequirements(IEnumerable<Requirement> requirements);
    void SaveKnowledgeArticles(IEnumerable<KnowledgeArticle> articles);
    void SaveRmAs(IEnumerable<Rma> rmAs);
    void SaveCapars(IEnumerable<Capar> capars);
    void SaveStandardDocuments(IEnumerable<StandardDocument> documents);

    /// <summary>Copies the source file into the storage folder and returns the stored metadata.</summary>
    StandardDocument StoreDocumentFile(string sourcePath, string fileTitle, string description, StandardCategory category);

    /// <summary>Returns the absolute path of a stored document, or null if the file is missing.</summary>
    string? GetDocumentPath(StandardDocument document);

    /// <summary>Deletes the stored binary file, if present.</summary>
    void DeleteDocumentFile(StandardDocument document);
}
