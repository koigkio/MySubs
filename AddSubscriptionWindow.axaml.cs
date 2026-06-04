using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Globalization;

namespace MySubs;

public partial class AddSubscriptionWindow : Window
{
    private MainWindow _mainWindow;
    
    public AddSubscriptionWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        
        var saveButton = this.FindControl<Button>("SaveButton");
        var cancelButton = this.FindControl<Button>("CancelButton");

        if (saveButton != null) saveButton.Click += SaveButton_Click;
        if (cancelButton != null) cancelButton.Click += CancelButton_Click;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        var nameInput = this.FindControl<TextBox>("NameInput");
        var priceInput = this.FindControl<TextBox>("PriceInput");
        var termInput = this.FindControl<TextBox>("TermInput");

        string name = nameInput?.Text ?? "";
        string priceText = priceInput?.Text ?? "";
        string term = termInput?.Text ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Введите название подписки!");
            return;
        }
        
        if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
        {
            ShowError("Введите корректную стоимость!");
            return;
        }
        
        if (price <= 0)
        {
            ShowError("Стоимость должна быть больше 0!");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(term))
        {
            ShowError("Введите срок (День, Неделя, Месяц или Год)!");
            return;
        }
        
        _mainWindow.AddSubscription(name, price, term);
        Close();
    }
    
    private void ShowError(string message)
    {
        var dialog = new Window
        {
            Title = "Ошибка",
            Width = 250,
            Height = 100,
            Content = new TextBlock { Text = message, Margin = new Avalonia.Thickness(20) }
        };
        dialog.ShowDialog(this);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e) => Close();
}