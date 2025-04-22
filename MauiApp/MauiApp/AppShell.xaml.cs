using MauiApp.Views;
using static Microsoft.Maui.Controls.Routing;

namespace MauiApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Регистрация маршрутов для навигации
            RegisterRoute(nameof(AddNotePage), typeof(AddNotePage));
            // Если бы была страница деталей:
            // Routing.RegisterRoute(nameof(NoteDetailPage), typeof(NoteDetailPage));
        }
    }
}
