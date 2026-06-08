using DeliveryService.DTO;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;
using System.Text.Json;


namespace DeliveryService.Utils
{
    public static class MapInitializer
    {
        /// <summary>
        /// Событие при выборе адреса на карте
        /// </summary>
        public static event Action<double, double, string>? AddressSelected;
        /// <summary>
        /// Событие при выборе курьера на карте
        /// </summary>
        public static event Func<List<List<double>>, Task>? CoordinatesRoute;

        /// <summary>
        /// Переменная, необходимая для проверки инциализирована ли карта или нет
        /// </summary>
        private static readonly HashSet<WebView2> _initializedMaps = new();

        public static void Reset(WebView2 map)
        {
            _initializedMaps.Remove(map);
        }


        public static async Task Initialize(WebView2 MapWebView)
        {
            string html = """ 
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <script src="https://api-maps.yandex.ru/2.1/?apikey=7884a1f6-e701-4ae1-bca9-d35b02adaf1e&lang=ru_RU"></script>
                <style>
                    html, body, #map {
                        width: 100%;
                        height: 100%;
                        margin: 0;
                        padding: 0;
                    }
                </style>
            </head>
            <body>
                <div id="map"></div>

                <script src="https://delivery.local/script.js"></script>
            
            </body>
            </html>
            """;
            await MapWebView.EnsureCoreWebView2Async();
            var utilsFolder = Path.Combine(AppContext.BaseDirectory, "Utils");
            MapWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "delivery.local",
                utilsFolder,
                CoreWebView2HostResourceAccessKind.Allow);

            if (_initializedMaps.Contains(MapWebView))
                return;
            _initializedMaps.Add(MapWebView);

            MapWebView.CoreWebView2.WebMessageReceived += (sender, args) =>
            {
                string json = args.WebMessageAsJson;
                if (json.Contains("routeCoordinates"))
                {
                    var routeData = JsonSerializer.Deserialize<CoordinatesDTO>(json);
                    CoordinatesRoute?.Invoke(routeData.coordinates);

                }
                if (json.Contains("mapClick"))
                {
                    var list = JsonSerializer.Deserialize<AddressDTO>(json);
                    AddressSelected?.Invoke(list.lat, list.lon, list.address);

                }
            };

            MapWebView.NavigateToString(html);
        }
    }
}
