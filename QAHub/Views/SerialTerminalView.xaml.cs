using System.Windows.Controls;

namespace QAHub.Views;

public partial class SerialTerminalView : UserControl
{
    public SerialTerminalView()
    {
        InitializeComponent();
        // DataContext is supplied by the DataTemplate mapping in App.xaml
        // (bound to MainViewModel.SerialTerminal), not created here.
    }
}
