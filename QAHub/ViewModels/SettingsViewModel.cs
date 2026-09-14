using System.Windows;
using System.Windows.Input;

namespace QAHub.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private readonly MainViewModel _mainViewModel;

    public SettingsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;

        ResetTestRunsCommand = new RelayCommand(_ => ResetTestRuns());
        ResetRequirementsCommand = new RelayCommand(_ => ResetRequirements());
        ResetProjectsCommand = new RelayCommand(_ => ResetProjects());
        ResetKnowledgeBaseCommand = new RelayCommand(_ => ResetKnowledgeBase());
        ResetAllCommand = new RelayCommand(_ => ResetAll());
    }

    public ICommand ResetTestRunsCommand { get; }
    public ICommand ResetRequirementsCommand { get; }
    public ICommand ResetProjectsCommand { get; }
    public ICommand ResetKnowledgeBaseCommand { get; }
    public ICommand ResetAllCommand { get; }

    private bool Confirm(string message)
    {
        return MessageBox.Show(message, "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning)
            == MessageBoxResult.Yes;
    }

    private void ResetTestRuns()
    {
        if (!Confirm("Delete all test runs? This cannot be undone.")) return;

        var runs = _mainViewModel.TestRuns.Runs;
        while (runs.Count > 0)
        {
            _mainViewModel.TestRuns.DeleteRunCommand.Execute(runs[0]);
        }
    }

    private void ResetRequirements()
    {
        if (!Confirm("Delete all requirements? This cannot be undone.")) return;

        var requirements = _mainViewModel.Requirements.Requirements;
        while (requirements.Count > 0)
        {
            _mainViewModel.Requirements.DeleteRequirementCommand.Execute(requirements[0]);
        }
    }

    private void ResetProjects()
    {
        if (!Confirm("Delete all projects? This cannot be undone.")) return;

        var projects = _mainViewModel.Projects.Projects;
        while (projects.Count > 0)
        {
            _mainViewModel.Projects.DeleteProjectCommand.Execute(projects[0]);
        }
    }

    private void ResetKnowledgeBase()
    {
        if (!Confirm("Delete all knowledge base articles? This cannot be undone.")) return;

        var articles = _mainViewModel.KnowledgeBase.Articles;
        while (articles.Count > 0)
        {
            _mainViewModel.KnowledgeBase.DeleteArticleCommand.Execute(articles[0]);
        }
    }

    private void ResetAll()
    {
        if (!Confirm("Delete ALL data across Test Runs, Requirements, Projects, and Knowledge Base? This cannot be undone.")) return;

        var runs = _mainViewModel.TestRuns.Runs;
        while (runs.Count > 0) _mainViewModel.TestRuns.DeleteRunCommand.Execute(runs[0]);

        var requirements = _mainViewModel.Requirements.Requirements;
        while (requirements.Count > 0) _mainViewModel.Requirements.DeleteRequirementCommand.Execute(requirements[0]);

        var projects = _mainViewModel.Projects.Projects;
        while (projects.Count > 0) _mainViewModel.Projects.DeleteProjectCommand.Execute(projects[0]);

        var articles = _mainViewModel.KnowledgeBase.Articles;
        while (articles.Count > 0) _mainViewModel.KnowledgeBase.DeleteArticleCommand.Execute(articles[0]);
    }
}
