using DeliveryService.Models;
using DeliveryService.Utils;


namespace DeliveryService.Services
{
    public class SimulationService
    {
        private readonly CourierService _courierService;
        private readonly SessionService _sessionService;
        private readonly OrderService _orderService;
        private CancellationTokenSource? _simulationCts;

        public event Action<double, double>? CourierMoved;
        public event Action? CourierFinal;

        public SimulationService(CourierService courierService, SessionService sessionService, OrderService orderService)
        {
            _courierService = courierService;
            _sessionService = sessionService;
            _orderService = orderService;
            Logger.LogDebug("SimulationService инициализирован");
        }

        /// <summary>
        /// Симуляция маршрута
        /// </summary>
        /// <param name="points">Список списков координат маршрута</param>
        /// <returns></returns>
        public async Task StartAsync(List<List<double>> points, Courier courier)
        {
            if (courier == null)
            {
                Logger.LogError("StartAsync вызван с null-курьером");
                return;
            }

            if (points == null || points.Count == 0)
            {
                Logger.LogWarning($"StartAsync для курьера {courier.Id} вызван с пустым маршрутом");
                return;
            }

            Logger.LogInfo($"Начало симуляции маршрута для курьера {courier.Id}. Количество точек маршрута: {points.Count}");

            try
            {
                _simulationCts?.Cancel();
                _simulationCts?.Dispose();
                _simulationCts = new CancellationTokenSource();
                var token = _simulationCts.Token;

                var startIndex = NearestIndexFinder(points, courier.Current_Lat, courier.Current_Lon);
                Logger.LogDebug($"Курьер {courier.Id} начинает движение с точки {startIndex} (текущие координаты: {courier.Current_Lat}, {courier.Current_Lon})");
                
                var remaining = points.Skip(startIndex);
                int pointCount = 0;
                int totalPoints = remaining.Count();

                foreach (var point in remaining)
                {
                    if (token.IsCancellationRequested)
                    {
                        Logger.LogInfo($"Симуляция для курьера {courier.Id} отменена пользователем");
                        return;
                    }

                    pointCount++;
                    var lat = point[0];
                    var lon = point[1];

                    Logger.LogDebug($"Курьер {courier.Id} перемещается к точке {pointCount}/{totalPoints}: ({lat}, {lon})");

                    CourierMoved?.Invoke(lat, lon);

                    courier.Current_Lat = lat;
                    courier.Current_Lon = lon;
                    await _courierService.Update(courier);
                    await Task.Delay(600, token);
                }

                var orderPoint = remaining.Last();
                if (courier.Current_Lat == orderPoint[0] && courier.Current_Lon == orderPoint[1])
                {
                    Logger.LogInfo($"Курьер {courier.Id} достиг точки доставки. Завершение заказа.");

                    var order = await _orderService.FindOrderByCourierIdAsync(courier.Id);
                    if (order == null)
                    {
                        Logger.LogWarning($"Заказ для курьера {courier.Id} не найден при завершении доставки");
                        return;
                    }

                    Logger.LogInfo($"Заказ {order.Id} успешно доставлен курьером {courier.Id}");

                    order.Status = "Доставлен";
                    order.Courier = null;
                    await _orderService.Update(order);
                    await _orderService.AddToHistory(order, status: "Доставлен");
                    CourierFinal?.Invoke();

                    Logger.LogInfo($"Симуляция для курьера {courier.Id} успешно завершена");
                }
                else
                {
                    Logger.LogWarning($"Курьер {courier.Id} не достиг конечной точки маршрута. Текущие координаты: ({courier.Current_Lat}, {courier.Current_Lon}), целевые: ({orderPoint[0]}, {orderPoint[1]})");
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogInfo($"Симуляция для курьера {courier.Id} была отменена");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка во время симуляции маршрута для курьера {courier.Id}", ex);
                throw;
            }
        }

        /// <summary>
        /// Остановка симуляции
        /// </summary>
        public void Stop()
        {
            Logger.LogInfo("Получен запрос на остановку симуляции");
            
            try
            {
                _simulationCts?.Cancel();
                _simulationCts?.Dispose();
                _simulationCts = null;
                Logger.LogDebug("Симуляция успешно остановлена");
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при остановке симуляции", ex);
            }
        }

        /// <summary>
        /// Функция, ищущая ближайшую точку к курьеру 
        /// </summary>
        /// <param name="points">Координаты</param>
        /// <param name="curLat">Lat курьера</param>
        /// <param name="curLon">Lon курьера</param>
        /// <returns>Возвращает индекс ближайшей пары точек</returns>
        private int NearestIndexFinder(List<List<double>> points, double curLat, double curLon)
        {
            if (points == null || points.Count == 0)
            {
                Logger.LogWarning("NearestIndexFinder вызван с пустым списком точек");
                return -1;
            }

            Logger.LogDebug($"Поиск ближайшей точки для координат ({curLat}, {curLon}) среди {points.Count} точек");

            int bestIndex = 0;
            double bestDist = double.MaxValue;

            try
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    double cLat = points[i][0];
                    double cLon = points[i][1];

                    double latDiff = cLat - curLat;
                    double lonDiff = cLon - curLon;
                    var distance = (latDiff * latDiff) + (lonDiff * lonDiff);

                    if (distance < bestDist)
                    {
                        bestDist = distance;
                        bestIndex = i;
                    }
                }

                Logger.LogDebug($"Найдена ближайшая точка: индекс {bestIndex}, координаты ({points[bestIndex][0]}, {points[bestIndex][1]}), расстояние {bestDist:F2}");
                return bestIndex;
            }
            catch (Exception ex)
            {
                Logger.LogError("Ошибка при поиске ближайшей точки маршрута", ex);
                return 0;
            }
        }
    }
}