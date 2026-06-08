using DeliveryService.Data;
using DeliveryService.Repositories;
using DeliveryService.Services;
using DeliveryService.ViewModels;
using DeliveryService.Views;
using DeliveryService.Utils; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace DeliveryService
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider? Services { get; private set; }
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                // ========== 1. ИНИЦИАЛИЗАЦИЯ ЛОГГЕРА ==========
                // Настройка логгера с ротацией и структурированным форматом
                Logger.Configure(
                    logDirectory: "logs",
                    enableConsole: true,        
                    enableFile: true,           
                    maxFileSizeMB: 10,          
                    maxArchiveFiles: 5          
                );
                
                // Выводит информацию о конфигурации логов
                Logger.LogInfo("=========================================");
                Logger.LogInfo("ЗАПУСК ПРИЛОЖЕНИЯ DELIVERY SERVICE");
                Logger.LogInfo($"Время запуска: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Logger.LogInfo($"Конфигурация логов: размер файла=10MB, архивов=5");
                Logger.LogInfo("=========================================");
                
                // Показывает информацию о логах (опционально)
                Logger.ShowLogInfo();
                
                // ========== 2. ЗАГРУЗКА КОНФИГУРАЦИИ ==========
                Logger.LogInfo("Загрузка конфигурации из appsettings.json");
                var config = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();
                
                Logger.LogDebug("Конфигурация успешно загружена");
                
                // ========== 3. НАСТРОЙКА DI КОНТЕЙНЕРА ==========
                Logger.LogInfo("Настройка DI контейнера");
                var services = new ServiceCollection();
                
                // БД
                Logger.LogDebug("Настройка подключения к базе данных");
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(config.GetConnectionString("Default")));
                
                // Репозитории
                Logger.LogDebug("Регистрация репозиториев");
                services.AddScoped<OrderRepository>();
                services.AddScoped<CourierRepository>();
                services.AddScoped<ClientRepository>();
                services.AddScoped<FoodCategoryRepository>();
                services.AddScoped<FoodRepository>();
                services.AddScoped<BasketRepository>();
                
                // Сервисы
                Logger.LogDebug("Регистрация сервисов");
                services.AddSingleton<SessionService>();
                services.AddSingleton<WindowsService>();
                services.AddScoped<SimulationService>();
                services.AddScoped<OrderService>();
                services.AddScoped<CourierService>();
                services.AddScoped<ClientService>();
                services.AddScoped<FoodCategoryService>();
                services.AddScoped<FoodService>();
                services.AddScoped<BasketService>();
                
                // ViewModels
                Logger.LogDebug("Регистрация ViewModels");
                services.AddTransient<MainWindowModel>();
                services.AddTransient<ListCouriersViewModel>();
                services.AddTransient<OrderListViewModel>();
                services.AddTransient<NewOrderViewModel>();
                services.AddTransient<RegistrationCourierModel>();
                services.AddTransient<DispatcherViewModel>();
                services.AddTransient<MenuViewModel>();
                services.AddTransient<EntranceViewModel>();
                services.AddTransient<RegistrationViewModel>();
                services.AddTransient<MenuViewModel>();
                services.AddTransient<OrderAcceptViewModel>();
                services.AddTransient<AuthorizationViewModel>();
                
                // View
                Logger.LogDebug("Регистрация View");
                services.AddTransient<MainWindow>();
                services.AddTransient<NewOrderView>();
                services.AddTransient<RegistrationCourier>();
                services.AddTransient<EntranceView>();
                services.AddTransient<MenuView>();
                services.AddTransient<OrderAcceptView>();
                
                //контейнер
                Logger.LogInfo("Построение ServiceProvider");
                Services = services.BuildServiceProvider();
                
                // ========== 4. МИГРАЦИЯ БАЗЫ ДАННЫХ ==========
                Logger.LogInfo("Проверка и применение миграций базы данных");
                try
                {
                    using var scope = Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                    Logger.LogInfo("Миграции БД успешно применены");
                }
                catch (Exception ex)
                {
                    Logger.LogError("Ошибка при применении миграций базы данных", ex);
                    throw;
                }
                
                // ========== 5. ЗАПУСК ГЛАВНОГО ОКНА ==========
                Logger.LogInfo("Создание и отображение EntranceView");
                var startupScope = Services.CreateScope();
                var win = startupScope.ServiceProvider.GetRequiredService<EntranceView>();
                
                win.Closed += (_, _) => 
                {
                    Logger.LogDebug("Закрытие EntranceView, освобождение ресурсов scope");
                    startupScope.Dispose();
                };
                
                win.Show();
                Logger.LogInfo("EntranceView успешно отображено");
            }
            catch (Exception ex)
            {
                // Логирует ошибку
                Logger.LogError($"КРИТИЧЕСКАЯ ОШИБКА при запуске приложения: {ex.Message}", ex);
                
                // Показывает пользователю сообщение об ошибке
                MessageBox.Show($"Не удалось запустить приложение.\nОшибка: {ex.Message}\n\n" +
                              $"Детали ошибки записаны в лог.\n" +
                              $"Лог-файл находится в папке: logs\\", 
                              "Ошибка запуска", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
                
                Shutdown();
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            Logger.LogInfo("=========================================");
            Logger.LogInfo("ЗАВЕРШЕНИЕ ПРИЛОЖЕНИЯ DELIVERY SERVICE");
            Logger.LogInfo($"Время завершения: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Logger.LogInfo($"Код выхода: {e.ApplicationExitCode}");
            Logger.LogInfo("=========================================");
            
            base.OnExit(e);
        }
        
        protected override void OnDispatcherUnhandledException(System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogError($"НЕОБРАБОТАННОЕ ИСКЛЮЧЕНИЕ в UI потоке: {e.Exception.Message}", e.Exception);
            
            MessageBox.Show($"Произошла непредвиденная ошибка:\n{e.Exception.Message}\n\n" +
                          $"Детали ошибки записаны в лог.\n" +
                          $"Лог-файл находится в папке: logs\\",
                          "Ошибка", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
            
            e.Handled = false; // разрешение приложению завершиться
        }
    }
}