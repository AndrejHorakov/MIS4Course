using System.Collections.Generic;
using System.Threading.Tasks;
using MauiApp.Models;

namespace MauiApp.Interfaces
{
    // Интерфейс для операций с данными
    public interface IDataService
    {
        Task InitializeAsync(); // Метод для инициализации (создание таблиц и т.д.)

        // Категории
        Task<List<Category>> GetCategoriesAsync();
        Task<Category> GetCategoryAsync(int id);
        Task<int> SaveCategoryAsync(Category category);
        Task<int> DeleteCategoryAsync(Category category);

        // Заметки
        Task<List<Note>> GetNotesWithCategoryAsync(); // Уже включает имя категории
        Task<Note> GetNoteAsync(int id);
        Task<int> SaveNoteAsync(Note note);
        Task<int> DeleteNoteAsync(Note note);
    }
}