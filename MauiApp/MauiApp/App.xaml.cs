using System;
using MauiApp.Models;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs; // Для ActionTapped
using MauiApp.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls; // Для навигации

namespace MauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();

        // Подписываемся на событие нажатия на уведомление
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
    }
        
    private async void OnNotificationActionTapped(NotificationActionEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"Notification Tapped: ID={e.Request.NotificationId}, ActionId={e.ActionId}, Data='{e.Request.ReturningData}'");

            if (e.Request.ReturningData != null && int.TryParse(e.Request.ReturningData, out int noteId) && noteId > 0)
            {
                // Получили ID заметки, теперь нужно перейти к ней.
                // ВАЖНО: Навигация должна происходить на основном потоке UI.
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Здесь логика перехода к странице деталей заметки (которой у нас пока нет)
                    // Или можно просто перейти на главную страницу и выделить элемент (сложнее)
                    // Пока просто покажем сообщение
                    await Shell.Current.DisplayAlert("Напоминание", $"Нажато уведомление для заметки ID: {noteId}", "OK");

                    // Пример навигации к странице деталей (если бы она была)
                    // var navigationParameter = new Dictionary<string, object> { { "NoteId", noteId } };
                    // await Shell.Current.GoToAsync($"{nameof(NoteDetailPage)}", navigationParameter);
                });
            }
            else if (e.ActionId == (int)NotificationActions.TapActionId) // Проверяем, что это основной тап, а не кнопка внутри уведомления
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await Shell.Current.DisplayAlert("Напоминание", $"Нажато уведомление: {e.Request.Title}", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling notification tap: {ex.Message}");
        }
    }

    protected override void OnStart()
    {
        // Вызывается при старте приложения ПОСЛЕ конструктора
        base.OnStart();
        System.Diagnostics.Debug.WriteLine("App OnStart: Состояние должно быть восстановлено во ViewModel при ее создании.");
        // ViewModel (NotesViewModel) сама загрузит свое состояние из Application.Current.Properties при инициализации.
    }

    protected override void OnSleep()
    {
        // Вызывается, когда приложение уходит в фон
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("App OnSleep: Попытка сохранить состояние.");
        // Можно принудительно вызвать сохранение состояния из активной ViewModel,
        // но лучше сохранять состояние по мере его изменения (как сделано в NotesViewModel).
        // Если нужно глобальное сохранение здесь:
        // var mainViewModel = GetCurrentViewModel somehow...
        // mainViewModel?.SaveStateCommand.Execute(null);

        // Важно! Убедимся, что все изменения в Properties сохранены.
        // Хотя SavePropertiesAsync вызывается в ViewModel, можно продублировать для надежности.
        // Application.Current.SavePropertiesAsync();
    }

    protected override void OnResume()
    {
        // Вызывается при возвращении приложения из фона
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("App OnResume.");
    }
}