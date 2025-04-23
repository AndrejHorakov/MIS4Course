using System.Net.Http;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using MauiApp.Models;
using Newtonsoft.Json;
using System.Collections.Generic; // Добавлено для использования Dictionary

namespace MauiApp.Data;
public class FirebaseWindowsService : IFirebaseService
{
    private readonly FirestoreDb _db;
    private const string ProjectId = "notes-d6236"; // Replace with your Project ID

    public FirebaseWindowsService()
    {
        FirebaseApp.Create(new AppOptions()
        {
            Credential = GoogleCredential.FromFile(@"D:\University\MIS\MIS4Course\MauiApp\MauiApp\Platforms\Windows\firebase-adminsdk.json"),
            ProjectId = ProjectId
        });

        _db = FirestoreDb.Create(ProjectId);
    }

    // Пример: Получение данных
    public async Task<List<Note>> GetNotesAsync()
    {
        CollectionReference collection = _db.Collection("Notes"); // Replace "Notes" with your collection name
        QuerySnapshot snapshot = await collection.GetSnapshotAsync();
        List<Note> notes = new List<Note>();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            Note note = document.ConvertTo<Note>();
            if (note != null)
            {
                notes.Add(note);
            }
        }
        return notes;
    }


    // Добавление данных (Note)
    public async Task<string> AddNoteAsync(Note note)
    {
        CollectionReference collection = _db.Collection("Notes");
        DocumentReference document = await collection.AddAsync(note); // Firestore сам сгенерирует ID
        return document.Id; // Возвращаем ID созданного документа
    }

    // Изменение данных (Note)
    public async Task UpdateNoteAsync(string noteId, Note note)
    {
        DocumentReference document = _db.Collection("Notes").Document(noteId);
        await document.SetAsync(note, SetOptions.MergeAll); // Обновляем документ целиком, с возможностью добавления новых полей
    }

    // Удаление данных (Note)
    public async Task DeleteNoteAsync(string noteId)
    {
        DocumentReference document = _db.Collection("Notes").Document(noteId);
        await document.DeleteAsync();
    }

    // Получение данных (Note) по ID
    public async Task<Note> GetNoteAsync(string noteId)
    {
        DocumentReference document = _db.Collection("Notes").Document(noteId);
        DocumentSnapshot snapshot = await document.GetSnapshotAsync();
        if (snapshot.Exists)
        {
            Note note = snapshot.ConvertTo<Note>();
            return note;
        }
        else
        {
            return null; // Документ не найден
        }
    }

    public async Task<Note?> GetNoteByIdAsync(int id)
    {
        var notes = await GetNotesAsync();
        return notes.FirstOrDefault(x => x.Id == id);
    }
}
