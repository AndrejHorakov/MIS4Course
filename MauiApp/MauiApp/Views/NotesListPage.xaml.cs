using MauiApp.ViewModels;
using Microsoft.Maui.Controls;

namespace MauiApp.Views;

public partial class NotesListPage : ContentPage
{
    // Получаем ViewModel через DI или используем BindingContext, установленный в XAML
    private readonly NotesViewModel _viewModel;

    public NotesListPage(NotesViewModel viewModel) // Инъекция ViewModel
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Вызываем метод ViewModel при появлении страницы
        _viewModel.OnAppearing();
    }

    // --- Обработка выбора элемента (если не используется CommunityToolkit) ---
    /*
   private void NotesListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
   {
       if (e.SelectedItem != null && BindingContext is NotesViewModel vm)
       {
            // Вызываем команду вручную при выборе элемента
           if (vm.NoteSelectedCommand.CanExecute(e.SelectedItem))
           {
               vm.NoteSelectedCommand.Execute(e.SelectedItem);
           }

           // Сбрасываем выделение в ListView, чтобы событие срабатывало повторно
           ((ListView)sender).SelectedItem = null;
       }
   }
   */

    // Связываем событие ItemSelected в XAML с этим обработчиком, если нужно
    // <ListView ... ItemSelected="NotesListView_ItemSelected">
}