using System.Windows;
using QAHub.Services;
using QAHub.ViewModels;

namespace QAHub;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dataService = new JsonDataService();
        var mainWindow = new MainWindow(new MainViewModel(dataService));
        mainWindow.Show();
    }
}