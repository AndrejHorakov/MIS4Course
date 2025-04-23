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
            RegisterRoute(nameof(AddNotePage), typeof(AddNotePage));
            RegisterRoute(nameof(EditNotePage), typeof(EditNotePage));
            RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        }
    }
}
