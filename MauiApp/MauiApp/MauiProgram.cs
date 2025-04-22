using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui;
using MauiApp.Data;
using MauiApp.Interfaces;
using MauiApp.ViewModels;
using MauiApp.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;
using Plugin.LocalNotification;

namespace MauiApp
{
    public static class MauiProgram
    {
        public static Microsoft.Maui.Hosting.MauiApp CreateMauiApp()
        {
            var builder = Microsoft.Maui.Hosting.MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
// Регистрация сервиса данных (Singleton из-за природы SQLite)
            builder.Services.AddSingleton<IDataService, SqliteDataService>(); // Заменяем NotesDatabase на интерфейс и реализацию

            // Регистрация ViewModels и Pages
            builder.Services.AddSingleton<NotesViewModel>();
            builder.Services.AddTransient<AddNoteViewModel>();

            builder.Services.AddSingleton<NotesListPage>();
            builder.Services.AddTransient<AddNotePage>();

            // Регистрация сервисов для сенсоров (будет добавлено ниже)
            builder.Services.AddSingleton(Geolocation.Default);
            builder.Services.AddSingleton(MediaPicker.Default);
            builder.Services.AddSingleton(FilePicker.Default); // Может понадобиться для выбора фото
            builder.Services.AddSingleton(FileSystem.Current); // Для работы с файлами
            

            var app = builder.Build();

            // Выполняем асинхронную инициализацию сервиса данных после построения приложения
            // Это хороший способ выполнить начальную настройку БД
            // Используем ServiceProvider для получения экземпляра IDataService
            Task.Run((Action)(async () => await app.Services.GetRequiredService<IDataService>().InitializeAsync()))
                .GetAwaiter() // Используем GetAwaiter().GetResult() для синхронного ожидания в синхронном методе
                .GetResult(); // ОСТОРОЖНО: Блокировка потока! В MauiProgram это обычно допустимо.

            return app;
        }
    }
}
