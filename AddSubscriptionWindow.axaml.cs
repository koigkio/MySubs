using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Data.Sqlite;

namespace MySubs;

public partial class AddSubscriptionWindow : Window
{
    private SubscriptionItem? _editSub;
    
    public AddSubscriptionWindow() { InitializeComponent(); SetupEvents(); }
    public AddSubscriptionWindow(SubscriptionItem subscription) { InitializeComponent(); _editSub = subscription; SetupEvents(); LoadData(); this.Title = "✏️ Редактирование"; }
    
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    
    private void SetupEvents()
    {
        var save = this.FindControl<Button>("SaveButton");
        var cancel = this.FindControl<Button>("CancelButton");
        if (save != null) save.Click += Save_Click;
        if (cancel != null) cancel.Click += (s, e) => Close();
    }
    
    private void LoadData()
    {
        if (_editSub == null) return;
        this.FindControl<TextBox>("TitleInput")!.Text = _editSub.Title;
        this.FindControl<TextBox>("PriceInput")!.Text = _editSub.Price.ToString();
        this.FindControl<TextBox>("DescriptionInput")!.Text = _editSub.Description;
        this.FindControl<CheckBox>("AutoRenewCheck")!.IsChecked = _editSub.AutoRenew;
        var currencyBox = this.FindControl<ComboBox>("CurrencyInput");
        if (currencyBox != null) currencyBox.SelectedIndex = _editSub.Currency == "$" ? 1 : _editSub.Currency == "€" ? 2 : 0;
        var payBox = this.FindControl<ComboBox>("PayMethodInput");
        if (payBox != null) payBox.SelectedIndex = _editSub.PayMethod == "crypto" ? 1 : _editSub.PayMethod == "foreign_card" ? 2 : 0;
    }
    
    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var title = this.FindControl<TextBox>("TitleInput")?.Text ?? "";
            var priceText = this.FindControl<TextBox>("PriceInput")?.Text ?? "0";
            var description = this.FindControl<TextBox>("DescriptionInput")?.Text ?? "";
            var autoRenew = this.FindControl<CheckBox>("AutoRenewCheck")?.IsChecked == true;
            if (string.IsNullOrWhiteSpace(title)) { await ShowMessage("Ошибка", "Введите название"); return; }
            if (!double.TryParse(priceText, out double price)) { await ShowMessage("Ошибка", "Введите цену"); return; }
            
            string currency = "₽";
            var currencyBox = this.FindControl<ComboBox>("CurrencyInput");
            if (currencyBox?.SelectedIndex == 1) currency = "$";
            else if (currencyBox?.SelectedIndex == 2) currency = "€";
            
            string payMethod = "card";
            var payBox = this.FindControl<ComboBox>("PayMethodInput");
            if (payBox?.SelectedIndex == 1) payMethod = "crypto";
            else if (payBox?.SelectedIndex == 2) payMethod = "foreign_card";
            
            using var connection = new SqliteConnection("Data Source=mysubs.db");
            connection.Open();
            
            if (_editSub != null)
            {
                new SqliteCommand($"UPDATE Subscriptions SET Title='{title}', Price={price}, PayMethod='{payMethod}', Currency='{currency}', Description='{description}', AutoRenew={(autoRenew ? 1 : 0)} WHERE Id={_editSub.Id}", connection).ExecuteNonQuery();
            }
            else
            {
                new SqliteCommand($"INSERT INTO Subscriptions (Title, Price, PayMethod, Currency, Description, AutoRenew, StartDate, EndDate, IsActive) VALUES ('{title}', {price}, '{payMethod}', '{currency}', '{description}', {(autoRenew ? 1 : 0)}, date('now'), date('now', '+1 month'), 1)", connection).ExecuteNonQuery();
            }
            Close();
        }
        catch (Exception ex) { await ShowMessage("Ошибка", ex.Message); }
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