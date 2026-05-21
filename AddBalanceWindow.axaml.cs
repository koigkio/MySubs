using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MySubs;

public partial class AddBalanceWindow : Window
{
    public AddBalanceWindow()
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
        var summInput = this.FindControl<TextBox>("SummInput");
        var payInput = this.FindControl<TextBox>("PayInput");
        var valuteInput = this.FindControl<TextBox>("ValuteInput");

        string summ = SummInput?.Text ?? "";
        string payText = PayInput?.Text ?? "";
        string valute = ValuteInput?.Text ?? "";

        if (decimal.TryParse(payText, out decimal pay))
        {
            System.Diagnostics.Debug.WriteLine($"Сумма пополнения: {summ}, Способ оплаты: {pay}, Валюта: {valute}");
        }
         

        Close(); 
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(); 
    }
}
