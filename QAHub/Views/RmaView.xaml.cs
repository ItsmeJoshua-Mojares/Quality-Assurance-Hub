using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QAHub.ViewModels;

namespace QAHub.Views;

public partial class RmaView : UserControl
{
    private INotifyPropertyChanged? _vm;

    public RmaView()
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
            case nameof(RmaViewModel.IsProjectsView):
            case nameof(RmaViewModel.IsProjectItemsView):
            case nameof(RmaViewModel.IsReadOnlyView):
            case nameof(RmaViewModel.IsFormView):
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(PageScroller.ScrollToTop));
                if (e.PropertyName == nameof(RmaViewModel.IsProjectItemsView) && _vm is RmaViewModel rvm && rvm.IsProjectItemsView)
                    Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new Action(() =>
                    {
                        var items = rvm.Items;
                        var copy = items.ToList();
                        items.Clear();
                        foreach (var item in copy) items.Add(item);
                    }));
                break;
        }
    }
}