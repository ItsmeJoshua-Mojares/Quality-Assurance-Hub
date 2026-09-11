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

        ShowDashboardCommand = new RelayCommand(_ => CurrentViewModel = Dashboard);
        ShowTestCasesCommand = new RelayCommand(_ => CurrentViewModel = TestCases);
        ShowBugsCommand = new RelayCommand(_ => CurrentViewModel = Bugs);
        ShowSerialTerminalCommand = new RelayCommand(_ => CurrentViewModel = SerialTerminal);

        CurrentViewModel = Dashboard;
    }

    public DashboardViewModel Dashboard { get; }
    public TestCasesViewModel TestCases { get; }
    public BugsViewModel Bugs { get; }
    public SerialTerminalViewModel SerialTerminal { get; }

    public ObservableObject? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                Dashboard.Refresh();
            }
        }
    }

    public RelayCommand ShowDashboardCommand { get; }
    public RelayCommand ShowTestCasesCommand { get; }
    public RelayCommand ShowBugsCommand { get; }
    public RelayCommand ShowSerialTerminalCommand { get; }
}