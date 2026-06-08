using DeliveryService.Services;
using DeliveryService.Utils;
using DeliveryService.ViewModels;
using System.Globalization;
using System.Windows;


namespace DeliveryService.Views
{
    /// <summary>
    /// Interaction logic for OrderAcceptView.xaml
    /// </summary>
    public partial class OrderAcceptView : Window
    {
        private readonly SessionService _sessionService;
        private readonly SimulationService _simulationService;
        public OrderAcceptView(OrderAcceptViewModel vm, SessionService sessionService, SimulationService simulationService)
        {
            InitializeComponent();
            DataContext = vm;
            _sessionService = sessionService;
            _simulationService = simulationService;
            _simulationService.CourierMoved += OnCourierMoved;
            MapInitializer.CoordinatesRoute += OnRouteReceived;

        }

        private async void OnCourierMoved(double Lat, double Lon)
        {

            _ = await Map.CoreWebView2.ExecuteScriptAsync(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "MoveCourier({0}, {1})",
                    Lat,
                    Lon));
        }

        private async Task OnRouteReceived(List<List<double>> points)
        {
            if (_sessionService.CurrentOrder != null && _sessionService.CurrentOrder.Courier != null)
                await _simulationService.StartAsync(points, _sessionService.CurrentOrder.Courier);
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await MapInitializer.Initialize(Map);

            if (DataContext is OrderAcceptViewModel vm)
            {
                vm.CourierAssigned += AddCourierMark;
            }



        }
        private async Task AddCourierMark()
        {
            _ = await Map.CoreWebView2.ExecuteScriptAsync(
            string.Format(CultureInfo.InvariantCulture,
                "DrawRoute({0}, {1}, {2}, {3}, true)",
                _sessionService.CurrentOrder.Lat_From,
                _sessionService.CurrentOrder.Lon_From,
                _sessionService.CurrentOrder.Lat_To,
                _sessionService.CurrentOrder.Lon_To));
            _ = await Map.CoreWebView2.ExecuteScriptAsync(
            string.Format(CultureInfo.InvariantCulture,
                "AddMark({0}, {1})",
                _sessionService.CurrentOrder.Courier.Current_Lat, _sessionService.CurrentOrder.Courier.Current_Lon));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _simulationService.CourierMoved -= OnCourierMoved;
            Map.Dispose();
            _simulationService.Stop();

        }
    }
}
