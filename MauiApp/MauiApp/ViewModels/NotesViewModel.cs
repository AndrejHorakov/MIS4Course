using System.Collections.ObjectModel;
using System.Windows.Input;
using MauiApp.Data;
using MauiApp.Interfaces;
using MauiApp.Models;
using MauiApp.Views;

namespace MauiApp.ViewModels;

public class NotesViewModel : BaseViewModel
    {
        private readonly IDataService _dataService; // Внедряем зависимость через интерфейс
        private readonly IFirebaseService _firebaseService;
        public ObservableCollection<Note> Notes { get; }
        public ICommand LoadNotesCommand { get; }
        public ICommand AddNoteCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        // Команда для сохранения последней выбранной заметки (для state management)
        public ICommand NoteSelectedCommand { get; }


        // Свойство для хранения ID последней выбранной заметки (для state management)
        private int _lastSelectedNoteId = -1;
        public int LastSelectedNoteId
        {
            get => _lastSelectedNoteId;
            set
            {
                 if (_lastSelectedNoteId != value)
                 {
                    _lastSelectedNoteId = value;
                    OnPropertyChanged();
                    // Сохраняем состояние при изменении
                    SaveStateCommand.Execute(null);
                 }
            }
        }

        // Команда для сохранения состояния (может вызываться автоматически или вручную)
        public ICommand SaveStateCommand { get; }


        public NotesViewModel(IDataService dataService, 
        IFirebaseService firebaseWindowsService) // Конструктор принимает IDataService
        {
            _dataService = dataService; // Сохраняем сервис
            _firebaseService = firebaseWindowsService;

            Notes = new ObservableCollection<Note>();
            LoadNotesCommand = new Command(async () => await ExecuteLoadNotesCommand());
            AddNoteCommand = new Command(async () => await GoToAddNotePage());
            DeleteNoteCommand = new Command<Note>(async note => await ExecuteDeleteNoteCommand(note));
            NoteSelectedCommand = new Command<Note>(ExecuteNoteSelectedCommand);
            SaveStateCommand = new Command(ExecuteSaveStateCommand);

            LoadState(); // Загрузка состояния (без изменений)
        }

        async Task ExecuteLoadNotesCommand()
        {

            try
            {
                Notes.Clear();
                // Используем внедренный сервис
                var notesList = await _dataService.GetNotesWithCategoryAsync();
                // var notesList = await _firebaseService.GetNotesAsync();
                foreach (var note in notesList)
                {
                    Notes.Add(note);
                }

                // Попытка восстановить "выделение" после загрузки
                if (LastSelectedNoteId != -1)
                {
                   // В ListView нет прямого способа "выделить" элемент из ViewModel без привязки SelectedItem,
                   // но мы сохранили ID. Можно использовать его для навигации к деталям, если нужно.
                   System.Diagnostics.Debug.WriteLine($"State restored: Last selected note ID was {LastSelectedNoteId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notes: {ex.Message}");
                // Можно показать сообщение пользователю
            }
            finally
            {
                IsBusy = false;
            }
        }

        async Task GoToAddNotePage()
        {
            await Shell.Current.GoToAsync(nameof(AddNotePage));
        }

        async Task ExecuteDeleteNoteCommand(Note note)
        {
            if (note == null) return;

            bool confirm = await Shell.Current.DisplayAlert("Удалить?",
                $"Вы уверены, что хотите удалить заметку '{note.Title}'?", "Да", "Нет");
            if(confirm)
            {
                await _dataService.DeleteNoteAsync(note); // Используем сервис
                Notes.Remove(note);

                var documentNote = await _firebaseService.GetNoteByIdAsync(note.Id);

                if(documentNote != null)
                {    
                    await _firebaseService.DeleteNoteAsync(documentNote.DocumentId);
                }


                if (LastSelectedNoteId == note.Id) LastSelectedNoteId = -1;
            }
        }

        void ExecuteNoteSelectedCommand(Note note)
        {
             if (note != null)
             {
                 LastSelectedNoteId = note.Id;
                 System.Diagnostics.Debug.WriteLine($"Note selected: ID {note.Id}");
             }
        }

        // --- State Management --- (3 балла)
        const string LastSelectedNoteIdKey = "LastSelectedNoteId";

        void ExecuteSaveStateCommand()
        {
            try
            { 
                Application.Current.Resources[LastSelectedNoteIdKey] = LastSelectedNoteId;
                // Принудительное сохранение словаря Properties (важно для некоторых платформ/сценариев)
                System.Diagnostics.Debug.WriteLine($"State saved: LastSelectedNoteId = {LastSelectedNoteId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving state: {ex.Message}");
            }
        }

         void LoadState()
        {
            if (Application.Current.Resources.TryGetValue(LastSelectedNoteIdKey, out var savedId))
            {
                if (savedId is int id) // Проверяем тип
                {
                    LastSelectedNoteId = id;
                    System.Diagnostics.Debug.WriteLine($"State loaded: LastSelectedNoteId = {LastSelectedNoteId}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"State loaded but incorrect type: {savedId?.GetType().Name}");
                    LastSelectedNoteId = -1; // Сброс в случае некорректного типа
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No saved state found for LastSelectedNoteId.");
                LastSelectedNoteId = -1; // Устанавливаем значение по умолчанию
            }
        }

        // Метод для вызова при старте приложения
        public void OnAppearing()
        {
            IsBusy = true; // Чтобы индикатор загрузки показался сразу
             // Не загружаем состояние здесь, т.к. оно уже загружено в конструкторе
            // LoadState(); // Загружаем состояние
            LoadNotesCommand.Execute(null); // Загружаем данные
        }
    }