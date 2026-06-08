using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;

using DeliveryService.ViewModels;

namespace DeliveryService.Views
{
    /// <summary>
    /// Логика взаимодействия для MenuView.xaml
    /// </summary>

    public partial class MenuView : Window
    {

        public MenuView(MenuViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Closed += (s, e) => viewModel.Dispose();
        }

        private void ListView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender
            };
            ((Control)sender).RaiseEvent(eventArg);
            e.Handled = true;
        }
    }
}