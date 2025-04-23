using MauiApp.ViewModels;

namespace MauiApp.Views
{
    public partial class AddNotePage : ContentPage
    {
        private readonly AddNoteViewModel _viewModel;
        public AddNotePage(AddNoteViewModel viewModel) // Инъекция ViewModel
        {
            InitializeComponent();
            BindingContext = viewModel;
            _viewModel = viewModel;
        }

        // Обработчик для кнопки "Отмена"
        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            // Вернуться на предыдущую страницу
            await Shell.Current.GoToAsync("..");
        }

         protected override void OnAppearing()
        {
            base.OnAppearing();
            // Старт мониторинга акселерометра
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ShakeDetected += Accelerometer_ShakeDetected;
                    try
                    {
                         Accelerometer.Default.Start(SensorSpeed.Game); // Используйте Game или UI
                    }
                    catch (FeatureNotSupportedException)
                    {
                         NotifyShakeDetectorStatus("Акселерометр (старт): не поддерживается");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error starting accelerometer: {ex.Message}");
                        NotifyShakeDetectorStatus("Ошибка старта акселерометра");
                    }
                }
            }
            else
            {
                NotifyShakeDetectorStatus("Акселерометр не поддерживается");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Остановка мониторинга акселерометра
            if (Accelerometer.Default.IsSupported && Accelerometer.Default.IsMonitoring)
            {
                try
                {
                    Accelerometer.Default.ShakeDetected -= Accelerometer_ShakeDetected;
                    Accelerometer.Default.Stop();
                }
                catch (FeatureNotSupportedException)
                {
                     // Обычно здесь ошибка не возникает, если IsMonitoring == true
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping accelerometer: {ex.Message}");
                }
            }
        }

        // --- НОВОЕ: Обработчик события сенсора ---
        private void Accelerometer_ShakeDetected(object? sender, EventArgs e)
        {
            // Вызываем команду ViewModel в основном потоке
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_viewModel?.ShakeDetectedCommand?.CanExecute(null) ?? false)
                {
                    _viewModel.ShakeDetectedCommand.Execute(null);
                }
            });
        }

        // Вспомогательный метод для уведомления статуса через ViewModel (если нужно)
        private void NotifyShakeDetectorStatus(string message)
        {
             MainThread.BeginInvokeOnMainThread(() => {
                 if (_viewModel != null)
                     _viewModel.ShakeStatusMessage = message; // Обновляем свойство ViewModel
             });
        }

    }
}