using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MySubs;

public partial class AddSubscriptionWindow : Window
{
    public AddSubscriptionWindow()
    {
        InitializeComponent();
        
        var saveButton = this.FindControl<Button>("SaveButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (saveButton != null) saveButton.Click += SaveButton_Click;
        if (cancelButton != null) cancelButton.Click += CancelButton_Click;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var nameInput = this.FindControl<TextBox>("NameInput");
        var priceInput = this.FindControl<TextBox>("PriceInput");
        var termInput = this.FindControl<TextBox>("TermInput");

        string name = nameInput?.Text ?? "";
        string priceText = priceInput?.Text ?? "";
        string term = termInput?.Text ?? "";

        if (decimal.TryParse(priceText, out decimal price))
        {
            System.Diagnostics.Debug.WriteLine($"Подписка: {name}, Цена: {price}, Срок: {term}");
        }
        else
        {
            var dialog = new Window
            {
                Title = "Ошибка",
                Width = 250,
                Height = 100,
                Content = new TextBlock 
                { 
                    Text = "Введите корректную стоимость!",
                    Margin = new Avalonia.Thickness(20)
                }
            };
            dialog.ShowDialog(this);
            return;
        }

        Close(); 
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(); 
    }
}