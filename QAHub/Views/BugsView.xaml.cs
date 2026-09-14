using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (DataContext is not BugsViewModel viewModel) return;

        // Same reasoning as TestCasesView: defer until after the DataGrid
        // has actually written the new value into the bound Bug property.
        Dispatcher.BeginInvoke(new System.Action(viewModel.Save), DispatcherPriority.Background);
    }
}
