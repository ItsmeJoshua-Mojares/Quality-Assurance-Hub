using System.Windows.Controls;
using QAHub.ViewModels;

namespace QAHub.Views;

public partial class TestCasesView : UserControl
{
    public TestCasesView()
    {
        InitializeComponent();
    }

    private void Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (DataContext is TestCasesViewModel vm) vm.Save();
        }));
    }
}