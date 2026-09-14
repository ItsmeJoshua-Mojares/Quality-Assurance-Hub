using System.Windows.Controls;
using System.Windows.Threading;
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
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (DataContext is not TestCasesViewModel viewModel) return;

        // At this point in the DataGrid lifecycle, the edited value has not
        // yet been pushed into the bound TestCase property — that commit
        // happens right after this handler returns. Defer the save to the
        // next dispatcher pass so it captures the actual new value.
        Dispatcher.BeginInvoke(new System.Action(viewModel.Save), DispatcherPriority.Background);
    }
}
