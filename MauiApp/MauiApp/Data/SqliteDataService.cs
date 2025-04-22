
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiApp.Interfaces;
using MauiApp.Models;
using SQLite;

namespace MauiApp.Data;

// Реализация сервиса данных с использованием SQLite
public class SqliteDataService : IDataService // Реализуем интерфейс
{
    private SQLiteAsyncConnection _database;
    private bool _initialized = false; // Флаг инициализации

    // Путь к БД теперь определяется здесь
    private static string DbPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NotesApp.db3");

    // Конструктор (может быть пустым или принимать параметры конфигурации)
    public SqliteDataService()
    {
        // _database = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        // Инициализация соединения перенесена в InitializeAsync
    }

    // Метод для ленивой инициализации соединения и таблиц
    private async Task EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            // Инициализируем соединение
            _database = new SQLiteAsyncConnection(DbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            // Создаем таблицы
            await _database.CreateTableAsync<Category>();
            await _database.CreateTableAsync<Note>(); // SQLite-net-pcl НЕ добавляет колонки автоматически! См. примечание ниже.

            // Добавим дефолтные категории, если их нет
            if (await _database.Table<Category>().CountAsync() == 0)
            {
                await _database.InsertAsync(new Category { Name = "Общее" });
                await _database.InsertAsync(new Category { Name = "Работа" });
                await _database.InsertAsync(new Category { Name = "Личное" });
            }
            _initialized = true;
        }
    }

    // Реализация метода инициализации интерфейса
    public async Task InitializeAsync()
    {
        await EnsureInitializedAsync();
    }

    // --- Методы для Категорий (теперь не статические) ---
    public async Task<List<Category>> GetCategoriesAsync()
    {
        await EnsureInitializedAsync();
        return await _database.Table<Category>().ToListAsync();
    }

    public async Task<Category> GetCategoryAsync(int id)
    {
        await EnsureInitializedAsync();
        return await _database.Table<Category>().Where(i => i.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveCategoryAsync(Category category)
    {
        await EnsureInitializedAsync();
        if (category.Id != 0)
        {
            return await _database.UpdateAsync(category);
        }
        else
        {
            return await _database.InsertAsync(category);
        }
    }

    public async Task<int> DeleteCategoryAsync(Category category)
    {
        await EnsureInitializedAsync();
        return await _database.DeleteAsync(category);
    }


    // --- Методы для Заметок (теперь не статические) ---
    public async Task<List<Note>> GetNotesWithCategoryAsync()
    {
        await EnsureInitializedAsync();
        // Получаем все заметки и все категории
        var notes = await _database.Table<Note>().OrderByDescending(n => n.DateCreated).ToListAsync();
        var categories = await GetCategoriesAsync(); // Используем метод этого же сервиса
        var categoryDict = categories.ToDictionary(c => c.Id, c => c.Name);

        // Присваиваем имя категории каждой заметке
        foreach (var note in notes)
        {
            // *** Важное примечание по миграции ***
            // Если вы добавили новые колонки (Latitude, Longitude, ImagePath) в модель Note ПОСЛЕ
            // того, как база данных была создана, sqlite-net-pcl НЕ добавит их автоматически
            // при вызове CreateTableAsync. При чтении старых записей возникнет ошибка,
            // если эти поля не Nullable в модели. Для реального приложения нужна стратегия миграции.
            // В данном примере предполагается, что или БД создается заново, или поля Nullable.

            if (categoryDict.TryGetValue(note.CategoryId, out string categoryName))
            {
                note.CategoryName = categoryName;
            }
            else
            {
                note.CategoryName = "Без категории";
            }
        }
        return notes;
    }

    public async Task<Note> GetNoteAsync(int id)
    {
        await EnsureInitializedAsync();
        // Здесь может быть ошибка, если в БД нет новых колонок, а модель их ожидает
        try
        {
            return await _database.Table<Note>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }
        catch (SQLiteException ex)
        {
            System.Diagnostics.Debug.WriteLine($"SQLite error getting note {id}: {ex.Message}. Schema mismatch?");
            // В реальном приложении здесь нужна обработка или миграция
            return null; // Или выбросить исключение
        }
    }

    public async Task<int> SaveNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        if (note.Id != 0)
        {
            // Обновление может вызвать ошибку, если схема БД не совпадает с моделью
            return await _database.UpdateAsync(note);
        }
        else
        {
            note.DateCreated = DateTime.UtcNow;
            return await _database.InsertAsync(note);
        }
    }

    public async Task<int> DeleteNoteAsync(Note note)
    {
        await EnsureInitializedAsync();
        return await _database.DeleteAsync(note);
    }
}