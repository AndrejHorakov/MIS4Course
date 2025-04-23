using Google.Cloud.Firestore; // Обязательно добавь using!
using SQLite;

namespace MauiApp.Models;

[FirestoreData] // Обязательно добавь этот атрибут к классу!
public class Note
{
    [PrimaryKey, AutoIncrement]
    [FirestoreProperty] // Firestore не использует Id, но если нужно, можно указать
    public int Id { get; set; }

    [FirestoreProperty("Title")] // Если имя поля в Firestore "Title", то так и указываем
    public string Title { get; set; } = string.Empty;

    [FirestoreProperty("Content")]
    public string Content { get; set; } = string.Empty;

    [FirestoreProperty("DateCreated")]
    public DateTime DateCreated { get; set; }

    [FirestoreProperty("CategoryId")]
    public int CategoryId { get; set; }

    // --- Новые поля для сенсоров ---
    [FirestoreProperty("Latitude")]
    public double? Latitude { get; set; } // Nullable double

    [FirestoreProperty("Longitude")]
    public double? Longitude { get; set; } // Nullable double

    [FirestoreProperty("ImagePath")]
    public string ImagePath { get; set; } = string.Empty;

    // --- Не хранятся в Firestore ---
    [Ignore] // SQLite атрибут, не относится к Firestore
    [FirestoreProperty("CategoryName")] 
    public string CategoryName { get; set; } = string.Empty;

    [FirestoreDocumentId]
    [Ignore]
    public string DocumentId { get; set; }

    [Ignore] // Добавим поле для ImageSource в UI
    public ImageSource NoteImageSource { get; set; }

    [Ignore] // Поле для отображения геолокации
    public string LocationDisplay => (Latitude.HasValue && Longitude.HasValue) ? $"📍 ({Latitude:F4}, {Longitude:F4})" : "";
}
