using MauiApp.ViewModels;

namespace MauiApp.Views;

public partial class EditNotePage : ContentPage
{
    // Храним ссылку на ViewModel (не обязательно, но как в твоем примере)
    private readonly EditNoteViewModel _viewModel;

    // Конструктор принимает ViewModel через Dependency Injection
    public EditNotePage(EditNoteViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel; // Устанавливаем BindingContext
        _viewModel = viewModel;     // Сохраняем ссылку
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Вызываем метод ViewModel (если он есть и нужен)
        // ViewModel сама решает, что делать при появлении страницы
        _viewModel.OnAppearing();
    }
}