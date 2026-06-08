using DeliveryService.Commands;
using DeliveryService.Models;
using DeliveryService.Services;
using DeliveryService.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DeliveryService.ViewModels
{
    /// <summary>
    /// Логика взаимодействия пользователя и базы данных с ListCouriersView
    /// </summary>
    public class ListCouriersViewModel : BaseViewModel
    {
        private readonly WindowsService _windowsService;
        private readonly CourierService _courierService;

        /// <summary>
        /// Список всех курьеров
        /// </summary>
        private ObservableCollection<Courier> _couriers;
        /// <summary>
        /// Текущий выбранный курьер
        /// </summary>
        private Courier _selectedCourier;

        /// <summary>
        /// Количество всех курьеров
        /// </summary>
        private int _totalCount;
        /// <summary>
        /// Количество курьеров, которые сейчас в онлайн
        /// </summary>
        private int _onlineCount;
        /// <summary>
        /// Количество курьеров, которые сейчас в офлайн
        /// </summary>
        private int _offlineCount;
        /// <summary>
        /// Количество всех заказов курьеров
        /// </summary>
        private int _totalOrderCount;

        /// <summary>
        /// Список всех курьеров
        /// </summary>
        public ObservableCollection<Courier> Couriers
        {
            get => _couriers;
            set => SetProperty(ref _couriers, value);
        }
        /// <summary>
        /// Текущий выбранный курьер
        /// </summary>
        public Courier SelectedCourier
        {
            get => _selectedCourier;
            set => SetProperty(ref _selectedCourier, value);
        }

        /// <summary>
        /// Количество всех курьеров
        /// </summary>
        public int TotalCount
        {
            get => _totalOrderCount;
            set => SetProperty(ref _totalOrderCount, value);
        }
        /// <summary>
        /// Количество курьеров, которые сейчас в онлайн
        /// </summary>
        public int OnlineCount
        {
            get => _onlineCount;
            set => SetProperty(ref _onlineCount, value);
        }
        /// <summary>
        /// Количество курьеров, которые сейчас в офлайн
        /// </summary>
        public int OfflineCount
        {
            get => _offlineCount;
            set => SetProperty(ref _offlineCount, value);
        }
        /// <summary>
        /// Количество всех заказов курьеров
        /// </summary>
        public int TotalOrderCount
        {
            get => _totalOrderCount;
            set => SetProperty(ref _totalOrderCount, value);
        }

        /// <summary>
        /// Команда загрузки курьеров в таблицу
        /// </summary>
        public ICommand LoadCouriersCommand { get; }
        /// <summary>
        /// Команда смены статуса онлайн/офлайн для выбранного курьера (сейчас это двойной клик по нужному курьеру в View)
        /// </summary>
        public ICommand ToggleOnlineCommand { get; }
        /// <summary>
        /// Команда открытия окна регистрации курьера
        /// </summary>
        public ICommand AddCourierCommand { get; }
        /// <summary>
        /// Команда для удаления курьера
        /// </summary>
        public ICommand RemoveCourierCommand { get; }


        public ListCouriersViewModel(WindowsService windowsService, CourierService courierService)
        {
            _windowsService = windowsService;
            _courierService = courierService;

            Couriers = new ObservableCollection<Courier>();

            LoadCouriersCommand = new RelayCommandAsync(
                execute: () => TryRunTaskAsync(LoadCouriersAsync, "Ошибка загрузки"),
                canExecute: () => !IsBusy
            );

            ToggleOnlineCommand = new RelayCommandAsync(
                execute: async (parameter) =>
                {
                    if (parameter is int courierId)
                        await ToggleCourierStatusAsync(courierId);
                },
                canExecute: _ => !IsBusy
            );
            AddCourierCommand = new RelayCommand(() =>
            {
                if (_windowsService.OpenRegistrationCourier() == true)
                    LoadCouriersCommand.Execute(null);
            });

            RemoveCourierCommand = new RelayCommandAsync(
                execute: async (parameter) =>
                {
                    if (parameter is int courierId)
                        await RemoveCourierAsync(courierId);
                },
                canExecute: _ => !IsBusy
            );


        }

        private async Task RemoveCourierAsync(object id)
        {
            if (IsBusy) return;
            if (!int.TryParse(id?.ToString(), out int courierId))
                return;

            var success = await _courierService.RemoveCourierAsync(courierId);

            if (success == true)
                await LoadCouriersAsync();
            else
            {
                ErrorMessage = "Не удалось удалить курьера";
                await Task.Delay(3000);
                ErrorMessage = null;
            }
        }

        /// <summary>
        /// Загрузка данных о курьерах в список
        /// </summary>
        private async Task LoadCouriersAsync()
        {
            var all = await _courierService.GetAllAsync();
            Couriers.Clear();
            TotalOrderCount = 0;
            foreach (var c in all)
            {
                Couriers.Add(c);
                TotalOrderCount += c.Orders.Count;
            }

            TotalCount = Couriers.Count;
            OnlineCount = Couriers.Count(c => c.IsActive);
            OfflineCount = TotalCount - OnlineCount;
        }

        /// <summary>
        /// Смена статуса онлайн/офлайн у выбранного курьера
        /// </summary>
        /// <param name="id">Id курьера</param>
        private async Task ToggleCourierStatusAsync(object id)
        {
            if (!int.TryParse(id?.ToString(), out int courierId))
                return;

            bool success = await _courierService.ToggleCourierOnlineAsync(courierId);
            if (success)
                await LoadCouriersAsync();
            else
            {
                ErrorMessage = "Не удалось переключить статус";
                await Task.Delay(3000);
                ErrorMessage = null;
            }
        }

    }
}