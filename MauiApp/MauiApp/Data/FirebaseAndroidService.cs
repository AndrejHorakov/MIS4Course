using System.Reflection;
using System.Text.Json;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;

using MauiApp.Models;

namespace MauiApp.Data;

public interface IFirebaseInitializer
{
    void Initialize();
}


public class FirebaseAndroidService : IFirebaseService, IFirebaseInitializer
{
    private const string ProjectId = "notes-d6236";

    private FirestoreDb _db; // Make this a field

    public FirebaseAndroidService()
    {
        var cred = GetCredentialFromEmbeddedResource("MauiApp.Resources.Raw.sdk.json");
        FirebaseApp.Create(new AppOptions()
        {
            Credential = cred,
            ProjectId = ProjectId
        });

        _db = FirestoreDb.Create(ProjectId);
    }

    public void Initialize()
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(); // This reads from google-services.json on Android
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Firebase initialization error (Android): {ex.Message}");
            throw; // Re-throw to prevent further execution if initialization fails
        }
    }

        public GoogleCredential GetCredentialFromEmbeddedResource(string resourceName)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new System.IO.FileNotFoundException($"Embedded resource '{resourceName}' not found.");
                }
                GoogleCredential credential = GoogleCredential.FromStream(stream);
                return credential;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading credentials from embedded resource: {ex}");
            return null; // Or throw
        }
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
