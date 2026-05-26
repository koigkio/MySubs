using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Data.Sqlite;

namespace MySubs;

public partial class AddBalanceWindow : Window
{
    public AddBalanceWindow()
    {
        InitializeComponent();
        var save = this.FindControl<Button>("SaveButton");
        var cancel = this.FindControl<Button>("CancelButton");
        if (save != null) save.Click += Save_Click;
        if (cancel != null) cancel.Click += (s, e) => Close();
    }
    
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    
    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        var amountText = this.FindControl<TextBox>("AmountInput")?.Text ?? "0";
        var paymentMethod = this.FindControl<ComboBox>("PaymentMethodInput")?.SelectedItem?.ToString() ?? "Не указан";
        var description = this.FindControl<TextBox>("DescriptionInput")?.Text ?? "Пополнение баланса";
        
        if (!decimal.TryParse(amountText, out decimal amount) || amount <= 0)
        {
            await ShowMessage("Ошибка", "Введите сумму больше 0");
            return;
        }
        
        using var connection = new SqliteConnection("Data Source=mysubs.db");
        connection.Open();
        using var transaction = connection.BeginTransaction();
        
        new SqliteCommand($"UPDATE Settings SET Value = CAST(Value AS REAL) + {amount} WHERE Key='CurrentBalance'", connection, transaction).ExecuteNonQuery();
        new SqliteCommand($"INSERT INTO Balance (Amount, OperationType, Description) VALUES ({amount}, 'deposit', '{description} ({paymentMethod})')", connection, transaction).ExecuteNonQuery();
        
        transaction.Commit();
        await ShowMessage("Успех", $"Баланс пополнен на {amount} ₽");
        Close();
    }
    
    private async Task ShowMessage(string title, string msg)
    {
        var dlg = new Window { Title = title, Width = 300, Height = 120 };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = msg });
        var ok = new Button { Content = "OK" };
        ok.Click += (_, _) => dlg.Close();
        stack.Children.Add(ok);
        dlg.Content = stack;
        await dlg.ShowDialog(this);
    }
}