using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MySubs;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // Метод, который открывает новое окно по клику на кнопку
    public async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var popup = new AddSubscriptionWindow();
        await popup.ShowDialog(this);
    }
     public async void AddButton1_Click(object? sender, RoutedEventArgs e)
    {
        var popup = new AddBalanceWindow();
        await popup.ShowDialog(this);
    }
}
