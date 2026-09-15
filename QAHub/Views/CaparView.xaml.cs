using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QAHub.ViewModels;

namespace QAHub.Views;

public partial class CaparView : UserControl
{
    private INotifyPropertyChanged? _vm;

    public CaparView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm = e.NewValue as INotifyPropertyChanged;
        if (_vm != null)
            _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CaparViewModel.IsProjectsView):
            case nameof(CaparViewModel.IsProjectItemsView):
            case nameof(CaparViewModel.IsReadOnlyView):
            case nameof(CaparViewModel.IsFormView):
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(PageScroller.ScrollToTop));
                if (e.PropertyName == nameof(CaparViewModel.IsProjectItemsView) && _vm is CaparViewModel cvm && cvm.IsProjectItemsView)
                    Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                    {
                        var items = cvm.Items;
                        var copy = items.ToList();
                        items.Clear();
                        foreach (var item in copy) items.Add(item);
                    }));
                break;
        }
    }
}