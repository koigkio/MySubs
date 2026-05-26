using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Data.Sqlite;

namespace MySubs;

public partial class MainWindow : Window
{
    private ObservableCollection<SubscriptionItem> subscriptions = new();
    private readonly string connectionString = "Data Source=mysubs.db";
    private TextBlock? txtCurrentBalance;
    private TextBox? searchBox;
    private ListBox? listSubs;

    public MainWindow()
    {
        InitializeComponent();
        
        txtCurrentBalance = this.FindControl<TextBlock>("TxtCurrentBalance");
        searchBox = this.FindControl<TextBox>("SearchBox");
        listSubs = this.FindControl<ListBox>("ListSubs");
        
        if (listSubs != null) listSubs.ItemsSource = subscriptions;
        if (searchBox != null) searchBox.TextChanged += SearchBox_TextChanged;
        
        var addSubBtn = this.FindControl<Button>("AddSubscriptionButton");
        var addBalanceBtn = this.FindControl<Button>("AddBalanceButton");
        
        if (addSubBtn != null) addSubBtn.Click += AddButton_Click;
        if (addBalanceBtn != null) addBalanceBtn.Click += AddButton1_Click;
        
        var editMenuItem = this.FindControl<MenuItem>("EditMenuItem");
        var renewMenuItem = this.FindControl<MenuItem>("RenewMenuItem");
        var cancelMenuItem = this.FindControl<MenuItem>("CancelMenuItem");
        var payNowMenuItem = this.FindControl<MenuItem>("PayNowMenuItem");
        var deleteMenuItem = this.FindControl<MenuItem>("DeleteMenuItem");
        
        if (editMenuItem != null) editMenuItem.Click += EditSubscription_Click;
        if (renewMenuItem != null) renewMenuItem.Click += RenewSubscription_Click;
        if (cancelMenuItem != null) cancelMenuItem.Click += CancelSubscription_Click;
        if (payNowMenuItem != null) payNowMenuItem.Click += PayNowSubscription_Click;
        if (deleteMenuItem != null) deleteMenuItem.Click += DeleteSubscription_Click;
        
        CreateDatabase();
        LoadSubscriptions();
        LoadCurrentBalance();
    }
    
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    
    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        FilterSubscriptions();
    }
    
    private void FilterSubscriptions()
    {
        if (listSubs == null) return;
        
        string searchText = searchBox?.Text?.Trim().ToLower() ?? "";
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            listSubs.ItemsSource = subscriptions;
        }
        else
        {
            var filtered = subscriptions.Where(s => 
                s.Title.ToLower().Contains(searchText) ||
                s.Description.ToLower().Contains(searchText)).ToList();
            listSubs.ItemsSource = filtered;
        }
    }
    
    private void CreateDatabase()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        string sql = @"
            CREATE TABLE IF NOT EXISTS Subscriptions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Price REAL NOT NULL,
                PayMethod TEXT NOT NULL,
                Currency TEXT NOT NULL,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                TrialEndDate TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CancelRequestDate TEXT,
                Description TEXT,
                AutoRenew INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
            )";
        
        using var cmd = new SqliteCommand(sql, connection);
        cmd.ExecuteNonQuery();
        
        string balanceSql = @"
            CREATE TABLE IF NOT EXISTS Balance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Amount REAL NOT NULL,
                OperationType TEXT NOT NULL,
                Description TEXT,
                Date TEXT DEFAULT CURRENT_TIMESTAMP
            )";
        
        using var cmd2 = new SqliteCommand(balanceSql, connection);
        cmd2.ExecuteNonQuery();
        
        string settingsSql = @"
            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            )";
        
        using var cmd3 = new SqliteCommand(settingsSql, connection);
        cmd3.ExecuteNonQuery();
        
        string initSql = "INSERT OR IGNORE INTO Settings (Key, Value) VALUES ('CurrentBalance', '0')";
        using var cmd4 = new SqliteCommand(initSql, connection);
        cmd4.ExecuteNonQuery();
    }
    
    private void LoadSubscriptions()
    {
        subscriptions.Clear();
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        string sql = "SELECT Id, Title, Price, PayMethod, Currency, StartDate, EndDate, TrialEndDate, IsActive, CancelRequestDate, Description, AutoRenew FROM Subscriptions WHERE IsActive != -1 ORDER BY Id DESC";
        
        using var cmd = new SqliteCommand(sql, connection);
        using var reader = cmd.ExecuteReader();
        
        while (reader.Read())
        {
            var sub = new SubscriptionItem
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Price = reader.GetDouble(2),
                PayMethod = reader.GetString(3),
                Currency = reader.GetString(4),
                StartDate = DateTime.Parse(reader.GetString(5)),
                EndDate = DateTime.Parse(reader.GetString(6)),
                TrialEndDate = reader.IsDBNull(7) ? null : DateTime.Parse(reader.GetString(7)),
                IsActive = reader.GetInt32(8) == 1,
                CancelRequestDate = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9)),
                Description = reader.IsDBNull(10) ? "" : reader.GetString(10),
                AutoRenew = reader.GetInt32(11) == 1
            };
            subscriptions.Add(sub);
        }
        
        FilterSubscriptions();
    }
    
    private void LoadCurrentBalance()
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        string sql = "SELECT Value FROM Settings WHERE Key = 'CurrentBalance'";
        using var cmd = new SqliteCommand(sql, connection);
        var result = cmd.ExecuteScalar();
        
        decimal balance = result != null ? Convert.ToDecimal(result) : 0;
        if (txtCurrentBalance != null) txtCurrentBalance.Text = $"Баланс: {balance:F2} ₽";
    }
    
    public async void AddButton_Click(object? sender, RoutedEventArgs e)
    {
        var win = new AddSubscriptionWindow();
        await win.ShowDialog(this);
        LoadSubscriptions();
        LoadCurrentBalance();
    }
    
    public async void AddButton1_Click(object? sender, RoutedEventArgs e)
    {
        var win = new AddBalanceWindow();
        await win.ShowDialog(this);
        LoadCurrentBalance();
    }
    
    private async void EditSubscription_Click(object? sender, RoutedEventArgs e)
    {
        var selected = listSubs?.SelectedItem as SubscriptionItem;
        if (selected == null)
        {
            await ShowMessage("Внимание", "Выберите подписку");
            return;
        }
        
        var win = new AddSubscriptionWindow(selected);
        await win.ShowDialog(this);
        LoadSubscriptions();
        LoadCurrentBalance();
    }
    
    private async void RenewSubscription_Click(object? sender, RoutedEventArgs e)
    {
        var selected = listSubs?.SelectedItem as SubscriptionItem;
        if (selected == null) return;
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        string sql = "UPDATE Subscriptions SET EndDate = date(EndDate, '+1 month'), IsActive = 1, CancelRequestDate = NULL WHERE Id = @id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", selected.Id);
        cmd.ExecuteNonQuery();
        
        LoadSubscriptions();
        await ShowMessage("Успех", $"Подписка '{selected.Title}' продлена!");
    }
    
    private async void CancelSubscription_Click(object? sender, RoutedEventArgs e)
    {
        var selected = listSubs?.SelectedItem as SubscriptionItem;
        if (selected == null) return;
        
        if (selected.IsPendingCancel)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string sql = "UPDATE Subscriptions SET CancelRequestDate = NULL, IsActive = 1 WHERE Id = @id";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", selected.Id);
            cmd.ExecuteNonQuery();
            
            LoadSubscriptions();
            await ShowMessage("Успех", "Подписка восстановлена!");
        }
        else
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            string sql = "UPDATE Subscriptions SET CancelRequestDate = CURRENT_TIMESTAMP, IsActive = 0 WHERE Id = @id";
            using var cmd = new SqliteCommand(sql, connection);
            cmd.Parameters.AddWithValue("@id", selected.Id);
            cmd.ExecuteNonQuery();
            
            LoadSubscriptions();
            await ShowMessage("Успех", $"Подписка '{selected.Title}' будет отменена через 30 минут!");
        }
    }
    
    private async void PayNowSubscription_Click(object? sender, RoutedEventArgs e)
    {
        var selected = listSubs?.SelectedItem as SubscriptionItem;
        if (selected == null) return;
        
        if (selected.IsInTrial)
        {
            await ShowMessage("Пробный период", "Подписка в пробном периоде, платить не нужно");
            return;
        }
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        using var transaction = connection.BeginTransaction();
        
        try
        {
            string checkSql = "SELECT CAST(Value AS REAL) FROM Settings WHERE Key = 'CurrentBalance'";
            using var checkCmd = new SqliteCommand(checkSql, connection, transaction);
            decimal balance = Convert.ToDecimal(checkCmd.ExecuteScalar() ?? 0);
            
            if (balance < (decimal)selected.Price)
            {
                await ShowMessage("Ошибка", "Недостаточно средств на балансе");
                return;
            }
            
            string withdrawSql = "UPDATE Settings SET Value = CAST(Value AS REAL) - @price WHERE Key = 'CurrentBalance'";
            using var withdrawCmd = new SqliteCommand(withdrawSql, connection, transaction);
            withdrawCmd.Parameters.AddWithValue("@price", selected.Price);
            withdrawCmd.ExecuteNonQuery();
            
            string renewSql = "UPDATE Subscriptions SET EndDate = date(EndDate, '+1 month'), LastPaymentDate = CURRENT_TIMESTAMP WHERE Id = @id";
            using var renewCmd = new SqliteCommand(renewSql, connection, transaction);
            renewCmd.Parameters.AddWithValue("@id", selected.Id);
            renewCmd.ExecuteNonQuery();
            
            transaction.Commit();
            
            LoadSubscriptions();
            LoadCurrentBalance();
            await ShowMessage("Успех", $"Оплачено {selected.Price} {selected.Currency}");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            await ShowMessage("Ошибка", ex.Message);
        }
    }
    
    private async void DeleteSubscription_Click(object? sender, RoutedEventArgs e)
    {
        var selected = listSubs?.SelectedItem as SubscriptionItem;
        if (selected == null) return;
        
        var result = await ShowConfirm("Удаление", $"Удалить подписку '{selected.Title}'?");
        if (!result) return;
        
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        
        string sql = "UPDATE Subscriptions SET IsActive = -1 WHERE Id = @id";
        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@id", selected.Id);
        cmd.ExecuteNonQuery();
        
        LoadSubscriptions();
        await ShowMessage("Успех", "Подписка удалена");
    }
    
    private async Task<bool> ShowConfirm(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        
        var dialog = new Window
        {
            Title = title,
            Width = 300,
            Height = 130,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var stack = new StackPanel { Margin = new Avalonia.Thickness(10) };
        stack.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        
        var buttonPanel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
        var yesBtn = new Button { Content = "Да", Width = 60 };
        var noBtn = new Button { Content = "Нет", Width = 60 };
        
        yesBtn.Click += (_, _) => { tcs.SetResult(true); dialog.Close(); };
        noBtn.Click += (_, _) => { tcs.SetResult(false); dialog.Close(); };
        
        buttonPanel.Children.Add(yesBtn);
        buttonPanel.Children.Add(noBtn);
        stack.Children.Add(buttonPanel);
        dialog.Content = stack;
        
        await dialog.ShowDialog(this);
        return await tcs.Task;
    }
    
    private async Task ShowMessage(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 300,
            Height = 120,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        var stack = new StackPanel { Margin = new Avalonia.Thickness(10) };
        stack.Children.Add(new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var ok = new Button { Content = "OK", Margin = new Avalonia.Thickness(0, 10, 0, 0), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
        ok.Click += (_, _) => dialog.Close();
        stack.Children.Add(ok);
        dialog.Content = stack;
        
        await dialog.ShowDialog(this);
    }
}