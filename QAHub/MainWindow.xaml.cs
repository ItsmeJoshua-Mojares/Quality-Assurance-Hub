using System.IO;
using System.Windows;
using QAHub.ViewModels;

namespace QAHub;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = $"QA Hub \u00b7 build {File.GetLastWriteTime(typeof(MainWindow).Assembly.Location):dd/MM HH:mm}";
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}