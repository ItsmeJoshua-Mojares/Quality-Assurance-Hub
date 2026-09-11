using QAHub.Models;

namespace QAHub.Services;

public interface IDataService
{
    List<TestCase> LoadTestCases();
    List<Bug> LoadBugs();
    void SaveTestCases(IEnumerable<TestCase> testCases);
    void SaveBugs(IEnumerable<Bug> bugs);
}