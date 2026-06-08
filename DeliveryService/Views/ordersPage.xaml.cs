using System.Windows.Controls;


namespace DeliveryService.Views
{
    /// <summary>
    /// Interaction logic for ordersPage.xaml
    /// </summary>
    public partial class ordersPage : UserControl
    {
        public ordersPage()
        {
            InitializeComponent();
            Loaded += (sender, e) =>
            {
                combobox.SelectedIndex = 0;
            };
        }

    }
}
