using MauiApp.Models;

namespace MauiApp.Data;

public interface IFirebaseService
{
    Task<List<Note>> GetNotesAsync();
    Task<string> AddNoteAsync(Note note);
    Task UpdateNoteAsync(string noteId, Note note);
    Task DeleteNoteAsync(string noteId);
    Task<Note> GetNoteAsync(string noteId);
    Task<Note?> GetNoteByIdAsync(int id);
}
