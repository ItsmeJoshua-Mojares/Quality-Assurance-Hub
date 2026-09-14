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
    void SaveTestCases(IEnumerable<TestCase> testCases);
    void SaveBugs(IEnumerable<Bug> bugs);
    void SaveProjects(IEnumerable<Project> projects);
    void SaveShipments(IEnumerable<Shipment> shipments);
}
