using QAHub.Services;

namespace QAHub.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private ObservableObject? _currentViewModel;

    public MainViewModel(IDataService dataService)
    {
        _dataService = dataService;

        Dashboard = new DashboardViewModel(this);
        TestCases = new TestCasesViewModel(_dataService, () => Dashboard.Refresh());
        Bugs = new BugsViewModel(_dataService, () => Dashboard.Refresh());
        SerialTerminal = new SerialTerminalViewModel();
        TestRuns = new TestRunsViewModel(_dataService);
        Requirements = new RequirementsViewModel(_dataService);
        Projects = new ProjectsViewModel(_dataService);
        Reports = new ReportsViewModel(this);
        KnowledgeBase = new KnowledgeBaseViewModel(_dataService);
        Settings = new SettingsViewModel(this);
        Shipments = new ShipmentsViewModel(_dataService, Projects);

        ShowDashboardCommand = new RelayCommand(_ => CurrentViewModel = Dashboard);
        ShowTestCasesCommand = new RelayCommand(_ => CurrentViewModel = TestCases);
        ShowTestRunsCommand = new RelayCommand(_ => CurrentViewModel = TestRuns);
        ShowBugsCommand = new RelayCommand(_ => CurrentViewModel = Bugs);
        ShowRequirementsCommand = new RelayCommand(_ => CurrentViewModel = Requirements);
        ShowProjectsCommand = new RelayCommand(_ => CurrentViewModel = Projects);
        ShowReportsCommand = new RelayCommand(_ => CurrentViewModel = Reports);
        ShowKnowledgeBaseCommand = new RelayCommand(_ => CurrentViewModel = KnowledgeBase);
        ShowSerialTerminalCommand = new RelayCommand(_ => CurrentViewModel = SerialTerminal);
        ShowSettingsCommand = new RelayCommand(_ => CurrentViewModel = Settings);
        ShowShipmentsCommand = new RelayCommand(_ => CurrentViewModel = Shipments);

        CurrentViewModel = Dashboard;
    }

    public DashboardViewModel Dashboard { get; }
    public TestCasesViewModel TestCases { get; }
    public BugsViewModel Bugs { get; }
    public SerialTerminalViewModel SerialTerminal { get; }
    public TestRunsViewModel TestRuns { get; }
    public RequirementsViewModel Requirements { get; }
    public ProjectsViewModel Projects { get; }
    public ReportsViewModel Reports { get; }
    public KnowledgeBaseViewModel KnowledgeBase { get; }
    public SettingsViewModel Settings { get; }
    public ShipmentsViewModel Shipments { get; }

    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                Dashboard.Refresh();
                if (value == Reports) Reports.Refresh();
            }
        }
    }

    public RelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowTestCasesCommand { get; }
    public RelayCommand ShowTestRunsCommand { get; }
    public RelayCommand ShowBugsCommand { get; }
    public RelayCommand ShowRequirementsCommand { get; }
    public RelayCommand ShowProjectsCommand { get; }
    public RelayCommand ShowReportsCommand { get; }
    public RelayCommand ShowKnowledgeBaseCommand { get; }
    public RelayCommand ShowSerialTerminalCommand { get; }
    public RelayCommand ShowSettingsCommand { get; }
    public RelayCommand ShowShipmentsCommand { get; }
}