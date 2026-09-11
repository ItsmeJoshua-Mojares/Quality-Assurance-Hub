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

        ShowDashboardCommand = new RelayCommand(_ => CurrentViewModel = Dashboard);
        ShowTestCasesCommand = new RelayCommand(_ => CurrentViewModel = TestCases);
        ShowBugsCommand = new RelayCommand(_ => CurrentViewModel = Bugs);

        CurrentViewModel = Dashboard;
    }

    public DashboardViewModel Dashboard { get; }
    public TestCasesViewModel TestCases { get; }
    public BugsViewModel Bugs { get; }

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
}