
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiApp.Models;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MauiApp.Interfaces;
using Plugin.LocalNotification;
using Plugin.LocalNotification.AndroidOption;

namespace MauiApp.ViewModels;

// Атрибут для приема параметра навигации (ID заметки)
[QueryProperty(nameof(NoteId), nameof(NoteId))]
public partial class EditNoteViewModel : ObservableObject
{
    private readonly IDataService _dataService;
    private readonly IGeolocation _geolocation;
    private readonly IMediaPicker _mediaPicker;
    private readonly IFileSystem _fileSystem;

    // Свойства для привязки к UI (похожи на AddNoteViewModel)
    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private Category? _selectedCategory; // Используем nullable тип

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private string _locationText = "Местоположение не добавлено";

    [ObservableProperty]
    private ImageSource? _noteImageSource;

    public string ReminderText => ReminderTime.HasValue 
        ? $"⏰ {ReminderTime:g}" 
        : "Напоминание не установлено";

    [ObservableProperty]
    private bool _isBusy;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReminderText))]
    private DateTime? _reminderTime; // Для уведомлений

    // Свойство для хранения ID заметки, полученного через навигацию
    [ObservableProperty]
    private int _noteId;

    // Свойство для хранения оригинальной заметки
    private Note? _currentNote;
    private string? _currentImagePath; // Храним путь к текущему изображению

    public EditNoteViewModel(IDataService dataService, IGeolocation geolocation, IMediaPicker mediaPicker, IFileSystem fileSystem)
    {
        _dataService = dataService;
        _geolocation = geolocation;
        _mediaPicker = mediaPicker;
        _fileSystem = fileSystem;

        // Команда сохранения должна быть доступна сразу, но может быть доп. логика
        // SaveNoteCommand = new AsyncRelayCommand(SaveNoteAsync, CanSaveNote);
        // DeleteNoteCommand = new AsyncRelayCommand(DeleteNoteAsync);
        // LoadCategoriesCommand = new AsyncRelayCommand(LoadCategoriesAsync);
        // GetLocationCommand = new AsyncRelayCommand(GetLocationAsync);
        // AttachPhotoCommand = new AsyncRelayCommand(AttachPhotoAsync);
        // SetReminderCommand = new RelayCommand(SetReminder); // Заглушка

        // Загружаем категории при создании ViewModel
        _ = LoadCategoriesAsync();
    }

    // Метод, вызываемый после установки NoteId через QueryProperty
    async partial void OnNoteIdChanged(int value)
    {
        // Загружаем данные конкретной заметки
        await LoadNoteAsync(value);
    }

    private async Task LoadNoteAsync(int noteId)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            _currentNote = await _dataService.GetNoteAsync(noteId);
            if (_currentNote != null)
            {
                Title = _currentNote.Title;
                Content = _currentNote.Content;
                // Находим и устанавливаем категорию в Picker
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == _currentNote.CategoryId);
                _currentImagePath = _currentNote.ImagePath; // Сохраняем текущий путь

                // Обновляем геолокацию
                if (_currentNote.Latitude.HasValue && _currentNote.Longitude.HasValue)
                {
                    LocationText = $"📍 {_currentNote.Latitude:F5}, {_currentNote.Longitude:F5}";
                }
                else
                {
                    LocationText = "Местоположение не добавлено";
                }

                 // Обновляем изображение
                if (!string.IsNullOrEmpty(_currentNote.ImagePath))
                {
                    // Важно: Убедитесь, что путь правильный (абсолютный или относительный к данным приложения)
                    NoteImageSource = ImageSource.FromFile(_currentNote.ImagePath);
                }
                else
                {
                    NoteImageSource = null;
                }
            }
            else
            {
                // Обработка случая, если заметка не найдена
                await Shell.Current.DisplayAlert("Ошибка", "Не удалось загрузить заметку.", "OK");
                await Shell.Current.GoToAsync(".."); // Возвращаемся назад
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"*** Ошибка загрузки заметки: {ex.Message}");
            await Shell.Current.DisplayAlert("Ошибка", "Произошла ошибка при загрузке заметки.", "OK");
        }
        finally
        {
            IsBusy = false;
            SaveNoteCommand.NotifyCanExecuteChanged();
        }
    }


    [RelayCommand]
    private async Task LoadCategoriesAsync()
    {
        try
        {
            var cats = await _dataService.GetCategoriesAsync();
            Categories.Clear();
            foreach (var cat in cats)
            {
                Categories.Add(cat);
            }
            // Перевыбираем категорию после загрузки, если _currentNote уже загружен
             if (_currentNote != null)
             {
                SelectedCategory = Categories.FirstOrDefault(c => c.Id == _currentNote.CategoryId);
                SaveNoteCommand.NotifyCanExecuteChanged();
             }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"*** Ошибка загрузки категорий: {ex.Message}");
            // Можно показать сообщение пользователю
        }
    }

    // Метод CanExecute для команды сохранения
    private bool CanSaveNote()
    {
        return !string.IsNullOrWhiteSpace(Title) && !IsBusy;
    }

    [RelayCommand(CanExecute = nameof(CanSaveNote))]
    private async Task SaveNoteAsync()
    {
        if (_currentNote == null || SelectedCategory == null)
        {
            await Shell.Current.DisplayAlert("Ошибка", "Данные заметки не загружены или категория не выбрана.", "OK");
            return;
        }

        IsBusy = true;
        SaveNoteCommand.NotifyCanExecuteChanged(); // Обновляем состояние кнопки

        try
        {
            // Обновляем данные _currentNote из свойств ViewModel
            _currentNote.Title = Title;
            _currentNote.Content = Content;
            _currentNote.CategoryId = SelectedCategory.Id;
            _currentNote.ImagePath = _currentImagePath; // Используем сохраненный путь
            // Геолокация и Напоминание обновляются внутри команд GetLocation/SetReminder (если они меняют _currentNote)

            if (ReminderTime.HasValue && _currentNote.Id > 0)
            {
                // Сначала отменим старое (если оно было с временным ID 0)
                // LocalNotificationCenter.Current.Cancel(0); // Не надежно, если было много несохраненных с напоминанием
                // Лучше перепланировать с новым ID
                await ScheduleNotification(_currentNote.Id, _currentNote.Title, _currentNote.Content ?? "Напоминание", ReminderTime.Value);
                System.Diagnostics.Debug.WriteLine($"Notification rescheduled with correct ID {_currentNote.Id}");
            }
            await _dataService.SaveNoteAsync(_currentNote);
            await Shell.Current.DisplayAlert("Успех", "Заметка успешно обновлена!", "OK");
            await Shell.Current.GoToAsync(".."); // Возвращаемся к списку
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"*** Ошибка сохранения заметки: {ex.Message}");
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось сохранить изменения.", "OK");
        }
        finally
        {
            IsBusy = false;
            SaveNoteCommand.NotifyCanExecuteChanged();
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

    [RelayCommand]
    private async Task DeleteNoteAsync()
    {
        if (_currentNote == null) return;

        bool confirmed = await Shell.Current.DisplayAlert("Подтверждение", $"Вы уверены, что хотите удалить заметку '{_currentNote.Title}'?", "Удалить", "Отмена");

        if (!confirmed) return;

        IsBusy = true;
        try
        {
            // Удаляем изображение, если оно есть
            if (!string.IsNullOrEmpty(_currentNote.ImagePath) && File.Exists(_currentNote.ImagePath))
            {
                File.Delete(_currentNote.ImagePath);
            }

            await _dataService.DeleteNoteAsync(_currentNote);
            await Shell.Current.DisplayAlert("Успех", "Заметка удалена.", "OK");
            await Shell.Current.GoToAsync(".."); // Возвращаемся к списку
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"*** Ошибка удаления заметки: {ex.Message}");
            await Shell.Current.DisplayAlert("Ошибка", "Не удалось удалить заметку.", "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // --- Команды для сенсоров и фото (почти как в AddNoteViewModel) ---

    [RelayCommand]
    private async Task GetLocationAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var location = await _geolocation.GetLastKnownLocationAsync();
            if (location == null)
            {
                location = await _geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            }

            if (location != null && _currentNote != null)
            {
                 _currentNote.Latitude = location.Latitude;
                 _currentNote.Longitude = location.Longitude;
                LocationText = $"📍 {location.Latitude:F5}, {location.Longitude:F5}";
            }
            else
            {
                LocationText = "Не удалось получить местоположение";
                if (_currentNote != null) {
                    _currentNote.Latitude = null;
                    _currentNote.Longitude = null;
                }
            }
        }
        catch (FeatureNotSupportedException)
        {
            LocationText = "Геолокация не поддерживается";
        }
        catch (PermissionException)
        {
            LocationText = "Нет разрешения на геолокацию";
             // Можно запросить разрешение
        }
        catch (Exception ex)
        {
            LocationText = "Ошибка геолокации";
            Debug.WriteLine($"*** Ошибка геолокации: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AttachPhotoAsync()
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
        try
        {
            FileResult? photo = null;
            try
            {
                var action = await Application.Current.MainPage.DisplayActionSheet("Добавить фото", "Отмена", null,
                    "Сделать фото", "Выбрать из галереи");


                if (action == "Сделать фото")
                {
                    if (_mediaPicker.IsCaptureSupported && cameraStatus == PermissionStatus.Granted)
                    {
                         photo = await _mediaPicker.CapturePhotoAsync();
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
                         photo = await _mediaPicker.PickPhotoAsync();
                     }
                     else
                     {
                         await Application.Current.MainPage.DisplayAlert("Ошибка",
                             "Нет разрешения на доступ к галерее.", "OK");
                     }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attach Photo Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Ошибка", "Не удалось прикрепить фото.", "OK");
            }
            finally
            {
                IsBusy = false;
            }

            if (photo != null && _currentNote != null)
            {
                IsBusy = true; // Показываем индикатор во время копирования

                // Удаляем старое фото, если оно было
                if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
                {
                    File.Delete(_currentImagePath);
                    _currentImagePath = null; // Сбрасываем путь
                }

                // Сохраняем новое фото
                string localFileName = $"{Guid.NewGuid()}_{photo.FileName}";
                string localFilePath = Path.Combine(_fileSystem.AppDataDirectory, localFileName);

                using var sourceStream = await photo.OpenReadAsync();
                using var localFileStream = File.Create(localFilePath);
                await sourceStream.CopyToAsync(localFileStream);

                // Обновляем свойства
                _currentNote.ImagePath = localFilePath; // Обновляем путь в заметке
                _currentImagePath = localFilePath;      // И в локальной переменной
                NoteImageSource = ImageSource.FromFile(localFilePath);

                 IsBusy = false; // Скрываем индикатор
            }
        }
        catch (FeatureNotSupportedException)
        {
            await Shell.Current.DisplayAlert("Ошибка", "Выбор/съемка фото не поддерживается на этом устройстве.", "OK");
        }
        catch (PermissionException)
        {
             await Shell.Current.DisplayAlert("Ошибка", "Нет разрешений на доступ к камере или галерее.", "OK");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"*** Ошибка прикрепления фото: {ex.Message}");
            await Shell.Current.DisplayAlert("Ошибка", "Произошла ошибка при работе с фото.", "OK");
            IsBusy = false;
        }
    }

    // Заглушка для напоминания
    [RelayCommand]
    private async Task SetReminder()
    {
         // Здесь будет логика выбора даты/времени и сохранения в _currentNote.ReminderDate
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
    
    public void OnAppearing()
    {
       
    }
}