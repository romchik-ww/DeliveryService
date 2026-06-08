using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DeliveryService.Commands
{
    /// <summary>
    /// Реализация команд, асинхронная
    /// </summary>
    public class RelayCommandAsync : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Func<object, bool> _canExecute;
        /// <summary>
        /// Выполняется ли сейчас команда
        /// </summary>
        private bool _isExecuting;

        /// <summary>
        /// Событие, возникающее при изменении возможности выполнения команды.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }


        public RelayCommandAsync(Func<object, Task> execute, Func<object, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (_ => true);
        }

        public RelayCommandAsync(Func<Task> execute, Func<bool> canExecute = null)
            : this(_ => execute(), _ => canExecute?.Invoke() ?? true) { }


        /// <summary>
        /// Может ли выполнится команда
        /// </summary>
        /// <param name="parameter">Параметр команды</param>
        /// <returns>Выполнилась ли команда</returns>
        public bool CanExecute(object parameter)
        {
            return !_isExecuting && _canExecute(parameter);
        }

        /// <summary>
        /// Выполнение команды
        /// </summary>
        /// <param name="parameter">Параметр команды</param>
        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute(parameter);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}