using System.Collections.ObjectModel;
using System.Windows.Input;
using MauiApp.Interfaces;
using MauiApp.Models;
using Plugin.LocalNotification; // Для уведомлений
using System.Runtime.InteropServices;
using Plugin.LocalNotification.AndroidOption;
using MauiApp.Data; // Для ActionTapped

namespace MauiApp.ViewModels
{
    public class AddNoteViewModel : BaseViewModel
    {
        private readonly IDataService _dataService; // Внедряем зависимость
        private readonly IFirebaseService _firebaseService; // зависимость
        private readonly IGeolocation _geolocation; // Для геолокации
        private readonly IMediaPicker _mediaPicker; // Для камеры
        private readonly IFileSystem _fileSystem; // Внедряем зависимость
        
        private string _title;
        private string _content;
        private Category _selectedCategory;
        private List<Category> _categories; // Список для пикера

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); ((Command)SaveNoteCommand).ChangeCanExecute(); }
        }

        public string Content
        {
            get => _content;
            set { _content = value; OnPropertyChanged(); }
        }

         public Category SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); ((Command)SaveNoteCommand).ChangeCanExecute(); }
        }

        // Используем ObservableCollection для Picker, если нужно динамическое обновление
        public ObservableCollection<Category> CategoriesSource { get; } = [];

        // --- Новые свойства для Сенсоров ---
        private double? _latitude;
        private double? _longitude;
        private string _imagePath;
        private ImageSource? _imageSource; // Для предпросмотра в UI
        private DateTime? _reminderTime; // Для уведомлений

        public double? Latitude { get => _latitude; set { _latitude = value; OnPropertyChanged();
            OnPropertyChanged(nameof(LocationText));
        } }
        public double? Longitude { get => _longitude; set { _longitude = value; OnPropertyChanged();
            OnPropertyChanged(nameof(LocationText));
        } }

        public string LocationText => (Latitude.HasValue && Longitude.HasValue)
            ? $"📍 {Latitude:F4}, {Longitude:F4}"
            : "Местоположение не добавлено";
        public string ImagePath { get => _imagePath; set { _imagePath = value; OnPropertyChanged(); } }
        public ImageSource? NoteImageSource { get => _imageSource; set { _imageSource = value; OnPropertyChanged(); } }
        public DateTime? ReminderTime { get => _reminderTime; set { _reminderTime = value; OnPropertyChanged();
            OnPropertyChanged(nameof(ReminderText));
        } }
        public string ReminderText => ReminderTime.HasValue 
            ? $"⏰ {ReminderTime:g}" 
            : "Напоминание не установлено";
        
        public ICommand SaveNoteCommand { get; }
        public ICommand LoadCategoriesCommand { get; }
        public ICommand GetLocationCommand { get; } // Новая команда
        public ICommand AttachPhotoCommand { get; } // Новая команда
        public ICommand SetReminderCommand { get; } // Новая команда

        public AddNoteViewModel(IDataService dataService, IGeolocation geolocation, IMediaPicker mediaPicker,
            IFileSystem fileSystem, IFirebaseService firebaseService)
        {
            _dataService = dataService;
            _fileSystem = fileSystem; // Сохраняем fileSystem
            _geolocation = geolocation; // Сохраняем сервис геолокации
            _mediaPicker = mediaPicker; // Сохраняем сервис медиапикера
            _firebaseService = firebaseService; // Сохраняем сервис firebase

            SaveNoteCommand = new Command(async () => await ExecuteSaveNoteCommand(), CanExecuteSaveNoteCommand);
            LoadCategoriesCommand = new Command(async () => await ExecuteLoadCategoriesCommand(), () => !IsBusy);
            SetReminderCommand = new Command(ExecuteSetReminderCommand, () => !IsBusy); // Инициализация команды
            
            GetLocationCommand = new Command(async () => await ExecuteGetLocationCommand(), () => !IsBusy);
            AttachPhotoCommand = new Command(async () => await ExecuteAttachPhotoCommand(), () => !IsBusy);

            LoadCategoriesCommand.Execute(null);
        }
        
        // --- Реализация команды Напоминания ---
        async void ExecuteSetReminderCommand()
        {
            // Здесь нужен UI для выбора даты и времени.
            // Для простоты примера - установим напоминание через 1 минуту.
            // В реальном приложении используйте DatePicker и TimePicker.

            bool confirm = await Application.Current.MainPage.DisplayAlert("Напоминание", "Установить напоминание через 1 минуту для этой заметки?", "Да", "Нет");

            if (confirm)
            {
                ReminderTime = DateTime.Now.AddMinutes(1);

                // Запланируем уведомление (лучше делать ПОСЛЕ сохранения заметки, чтобы иметь ID)
                // Но для демонстрации можно и здесь.
                if (string.IsNullOrWhiteSpace(Title)) // Требуем хотя бы заголовок
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Введите заголовок для установки напоминания.", "OK");
                    ReminderTime = null; // Сбрасываем, если не удалось
                }
            }
            else
            {
                ReminderTime = null; // Сброс, если пользователь отменил
            }
        }
        
        // Метод для планирования уведомления
        private async Task ScheduleNotification(int noteId, string title, string message, DateTime notifyTime)
        {
            // Проверка и запрос разрешений (особенно для Android 13+)
            if (await LocalNotificationCenter.Current.RequestNotificationPermission() == false)
            {
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Разрешение на показ уведомлений не предоставлено.", "OK");
                return;
            }


            try {
                var notification = new NotificationRequest
                {
                    NotificationId = noteId == 0 ? Random.Shared.Next(1000, 9999) : noteId, // Используем ID заметки или случайный ID
                    Title = title,
                    Subtitle = "Напоминание",
                    Description = message,
                    BadgeNumber = 1,
                    Schedule =
                    {
                        NotifyTime = notifyTime // Устанавливаем время срабатывания
                        // Можно настроить повторяющиеся уведомления NotifyRepeatInterval и т.д.
                    },
                    Android = // Настройки специфичные для Android
                    {
                        ChannelId = "note_reminders", // ID канала (важно для Android 8+)
                        IconSmallName = new AndroidIcon("ic_notification"), // Имя иконки из ресурсов drawable
                    },
                    // Добавляем данные, чтобы знать, какую заметку открыть при тапе

                };
                

                await LocalNotificationCenter.Current.Show(notification);
                System.Diagnostics.Debug.WriteLine($"Notification scheduled: ID {notification.NotificationId} for {notifyTime}");
                await Application.Current!.MainPage!.DisplayAlert("Успех", $"Напоминание установлено на {ReminderTime:g}", "OK");
            }
            catch (COMException ex) when (ex.HResult == -2147023728) // HRESULT для "Элемент не найден"
            {
                // Обработка ошибки отсутствия элемента
                Console.WriteLine("Ошибка уведомления: элемент не найден. Проверьте настройки Windows");
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось создать уведомление", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка уведомления: {ex.Message}");
                await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        async Task ExecuteGetLocationCommand()
        {
            if (IsBusy) return;
            IsBusy = true;
            ((Command)GetLocationCommand).ChangeCanExecute(); // Блокируем кнопку на время работы

            try
            {
                 // Проверка и запрос разрешений (важно!)
                 var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                 if (status != PermissionStatus.Granted)
                 {
                     status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                     if (status != PermissionStatus.Granted)
                     {
                         await Application.Current.MainPage.DisplayAlert("Ошибка",
                             "Разрешение на геолокацию не предоставлено.", "OK");
                         return;
                     }
                 }

                 // GeolocationRequest request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                 var location = await _geolocation.GetLocationAsync(new GeolocationRequest
                 {
                     DesiredAccuracy = GeolocationAccuracy.Medium,
                     Timeout = TimeSpan.FromSeconds(30)
                 });


                if (location != null)
                {
                    Latitude = location.Latitude;
                    Longitude = location.Longitude;
                    System.Diagnostics.Debug.WriteLine($"Location Found: {Latitude}, {Longitude}");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Ошибка", "Не удалось получить геолокацию.", "OK");
                }
            }
            catch (FeatureNotSupportedException fnsEx)
            {
                System.Diagnostics.Debug.WriteLine($"Geolocation Error: {fnsEx.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка",
                    "Геолокация не поддерживается на этом устройстве.", "OK");
            }
            catch (PermissionException pEx)
            {
                System.Diagnostics.Debug.WriteLine($"Geolocation Permission Error: {pEx.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Нет разрешения на использование геолокации.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geolocation Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Произошла ошибка при получении геолокации.",
                    "OK");
            }
            finally
            {
                IsBusy = false;
                ((Command)GetLocationCommand).ChangeCanExecute(); // Разблокируем кнопку
            }
        }
         
         async Task ExecuteAttachPhotoCommand()
        {
            if (IsBusy) return;


             // Проверка и запрос разрешений (КАМЕРА)
             var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
             if (cameraStatus != PermissionStatus.Granted)
             {
                 cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                 if (cameraStatus != PermissionStatus.Granted) {
                     await Application.Current.MainPage.DisplayAlert("Ошибка", "Разрешение на использование камеры не предоставлено.", "OK");
                     // Не выходим, пользователь может выбрать из галереи
                 }
             }

              // Проверка и запрос разрешений (ХРАНИЛИЩЕ/ГАЛЕРЕЯ) - зависит от версии Android
              PermissionStatus storageStatus;
              if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 13)
              {
                    storageStatus = await Permissions.CheckStatusAsync<Permissions.Media>(); // Или Permissions.Media
                    if(storageStatus != PermissionStatus.Granted)
                        storageStatus = await Permissions.RequestAsync<Permissions.Media>();
              }
              else
              {
                    storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                     if(storageStatus != PermissionStatus.Granted)
                        storageStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
              }

              if (storageStatus != PermissionStatus.Granted)
              {
                  await Application.Current.MainPage.DisplayAlert("Ошибка",
                      "Разрешение на доступ к галерее не предоставлено.", "OK");
                  // Не выходим, пользователь мог дать разрешение на камеру
              }


            IsBusy = true;
            ((Command)AttachPhotoCommand).ChangeCanExecute();

            try
            {
                var action = await Application.Current.MainPage.DisplayActionSheet("Добавить фото", "Отмена", null,
                    "Сделать фото", "Выбрать из галереи");

                FileResult? photoResult = null;

                if (action == "Сделать фото")
                {
                    if (_mediaPicker.IsCaptureSupported && cameraStatus == PermissionStatus.Granted)
                    {
                         photoResult = await _mediaPicker.CapturePhotoAsync();
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка",
                            "Камера недоступна или нет разрешения.", "OK");
                    }
                }
                else if (action == "Выбрать из галереи")
                {
                     if (storageStatus == PermissionStatus.Granted)
                     {
                         photoResult = await _mediaPicker.PickPhotoAsync();
                     }
                     else
                     {
                         await Application.Current.MainPage.DisplayAlert("Ошибка",
                             "Нет разрешения на доступ к галерее.", "OK");
                     }
                }

                if (photoResult != null)
                {
                    // Сохраняем фото в локальное хранилище приложения
                    var localFileName = $"{Guid.NewGuid()}_{photoResult.FileName}";
                    var localFilePath = Path.Combine(_fileSystem.AppDataDirectory, localFileName); // AppDataDirectory - надежное место

                    await using var sourceStream = await photoResult.OpenReadAsync();
                    await using var localFileStream = File.OpenWrite(localFilePath);

                    await sourceStream.CopyToAsync(localFileStream);

                    // Освобождаем предыдущее изображение, если оно было
                    // (Важно для управления памятью, особенно если изображения большие)
                    NoteImageSource = null;
                    ImagePath = localFilePath; // Сохраняем путь в ViewModel
                    NoteImageSource = ImageSource.FromFile(localFilePath); // Обновляем превью

                    System.Diagnostics.Debug.WriteLine($"Photo attached: {ImagePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Attach Photo Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Не удалось прикрепить фото.", "OK");
            }
            finally
            {
                IsBusy = false;
                ((Command)AttachPhotoCommand).ChangeCanExecute();
            }
        }
         
        async Task ExecuteLoadCategoriesCommand()
        {
             if (IsBusy) return;
             IsBusy = true;
             try
             {
                 var categoriesList = await _dataService.GetCategoriesAsync(); // Используем сервис
                 CategoriesSource.Clear();
                 foreach(var cat in categoriesList) { CategoriesSource.Add(cat); }
                 SelectedCategory = CategoriesSource.FirstOrDefault();
             }  
             catch(Exception ex)
             {
                 System.Diagnostics.Debug.WriteLine($"Error loading categories: {ex.Message}");
             }
             finally
             {
                 IsBusy = false;
             }
        }

        bool CanExecuteSaveNoteCommand()
        {
            // Можно сохранять только если есть заголовок и выбрана категория
            return !string.IsNullOrWhiteSpace(Title) && SelectedCategory != null;
        }
        
        async Task ExecuteSaveNoteCommand()
        {
            if (!CanExecuteSaveNoteCommand()) return;

            var newNote = new Note
            {
                Title = Title,
                Content = Content,
                CategoryId = SelectedCategory?.Id ?? 0,
                // Сохраняем данные сенсоров
                Latitude = Latitude,
                Longitude = Longitude,
                ImagePath = ImagePath
            };

            // Сначала сохраняем, чтобы получить ID (если это новая заметка)
            var id = await _dataService.SaveNoteAsync(newNote); // Получаем реальный ID
            
            var firebaseNote = await _firebaseService.GetNoteByIdAsync(id);
            if(firebaseNote == null)
            {
                await _firebaseService.AddNoteAsync(newNote);
            }
            else
            {
                await _firebaseService.UpdateNoteAsync(firebaseNote.DocumentId, newNote);
            }

            // Если было установлено время напоминания, ПЕРЕпланируем уведомление с правильным ID
            if (ReminderTime.HasValue && newNote.Id > 0)
            {
                // Сначала отменим старое (если оно было с временным ID 0)
                // LocalNotificationCenter.Current.Cancel(0); // Не надежно, если было много несохраненных с напоминанием
                // Лучше перепланировать с новым ID
                await ScheduleNotification(newNote.Id, newNote.Title, newNote.Content ?? "Напоминание", ReminderTime.Value);
                System.Diagnostics.Debug.WriteLine($"Notification rescheduled with correct ID {newNote.Id}");
            }

            await Shell.Current.GoToAsync("..");
        }
    }
}