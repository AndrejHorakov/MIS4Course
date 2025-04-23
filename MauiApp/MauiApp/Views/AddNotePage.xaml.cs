using MauiApp.ViewModels;

namespace MauiApp.Views
{
    public partial class AddNotePage : ContentPage
    {
        public AddNotePage(AddNoteViewModel viewModel) // Инъекция ViewModel
        {
            InitializeComponent();
            BindingContext = viewModel;
        }

        // Обработчик для кнопки "Отмена"
        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            // Вернуться на предыдущую страницу
            await Shell.Current.GoToAsync("..");
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Можно вызвать метод загрузки категорий здесь, если не сделали в конструкторе ViewModel
            // if (BindingContext is AddNoteViewModel vm && !vm.CategoriesSource.Any())
            // {
            //     vm.LoadCategoriesCommand.Execute(null);
            // }
        }
    }
}