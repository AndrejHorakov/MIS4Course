using System;
using Microsoft.Maui.Controls;
using SQLite;

namespace MauiApp.Models;

public class Note
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; }

    [Indexed]
    public int CategoryId { get; set; }

    // --- Новые поля для сенсоров ---
    public double? Latitude { get; set; } // Nullable double
    public double? Longitude { get; set; } // Nullable double
    public string ImagePath { get; set; } = string.Empty;// Путь к сохраненному файлу изображения

    // --- Не хранятся в БД ---
    [Ignore]
    public string CategoryName { get; set; } = string.Empty;

    [Ignore] // Добавим поле для ImageSource в UI
    public ImageSource NoteImageSource { get; set; }
    [Ignore] // Поле для отображения геолокации
    public string LocationDisplay => (Latitude.HasValue && Longitude.HasValue) ? $"📍 ({Latitude:F4}, {Longitude:F4})" : "";
}