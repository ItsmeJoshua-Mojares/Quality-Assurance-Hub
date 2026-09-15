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
    void SaveTestCases(IEnumerable<TestCase> testCases);
    void SaveBugs(IEnumerable<Bug> bugs);
    void SaveProjects(IEnumerable<Project> projects);
    void SaveShipments(IEnumerable<Shipment> shipments);
    void SaveTestRuns(IEnumerable<TestRun> testRuns);
    void SaveRequirements(IEnumerable<Requirement> requirements);
    void SaveKnowledgeArticles(IEnumerable<KnowledgeArticle> articles);
}
    void SaveRmAs(IEnumerable<Rma> rmAs);
    void SaveCapars(IEnumerable<Capar> capars);
}
