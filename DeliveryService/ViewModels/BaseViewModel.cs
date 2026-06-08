using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DeliveryService.ViewModels
{
    /// <summary>
    /// Базовый класс ViewModels
    /// </summary>
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Занят ли поток
        /// </summary>
        private bool _isBusy;
        /// <summary>
        /// Текст ошибки
        /// </summary>
        private string? _errorMessage;

        /// <summary>
        /// Занят ли поток
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }
        /// <summary>
        /// Текст ошибки
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Событие, возникающее при изменении свойства
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;


        /// <summary>
        /// Создание события для свойства модели
        /// </summary>
        /// <param name="propertyName">Название свойства модели</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        /// <summary>
        /// Изменение свойства модели
        /// </summary>
        /// <param name="property">Свойство модели</param>
        /// <param name="value">Значение для свойства</param>
        /// <param name="propertyName">Название свойства модели</param>
        /// <returns>Изменено ли значение свойства</returns>
        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(property, value))
                return false;

            property = value;

            if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                Application.Current.Dispatcher.Invoke(() => OnPropertyChanged(propertyName));
            else
                OnPropertyChanged(propertyName);

            return true;
        }

        /// <summary>
        /// Выполнение функции с проверкой ошибок
        /// </summary>
        /// <param name="task">Выполняемая функция</param>
        /// <param name="errorName">Название ошибки</param>
        /// <returns>Выполнилась ли функция</returns>
        protected async Task<bool> TryRunTaskAsync(Func<Task> task, string? errorName = null)
        {
            if (IsBusy)
                return false;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await task();
                return true;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{errorName ?? "Ошибка"}: {ex.Message}";
                var detail = ex.InnerException?.Message;
                Debug.WriteLine(detail);

                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}