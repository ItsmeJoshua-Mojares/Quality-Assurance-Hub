using System.Windows.Controls;
using QAHub.ViewModels;

namespace QAHub.Views;

public partial class BugsView : UserControl
{
    public BugsView()
    {
        InitializeComponent();
    }

    private void Grid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            if (DataContext is BugsViewModel vm) vm.Save();
        }));
    }
}