using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Media;
using System.Timers;
using System.Globalization;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Avalonia.Platform.Storage;

namespace MySubs;

public partial class MainWindow : Window
{
    private decimal _balance = 0;
    private string _subs = "";
    private string _history = "";
    private string _expenses = "";
    private StackPanel? _subsPanel;
    private TextBlock? _txtCurrentBalance;
    private TextBox? _searchBox;
    private Timer _timer;
    private DatabaseHelper _db;

    public MainWindow()
    {
        InitializeComponent();

        _txtCurrentBalance = this.FindControl<TextBlock>("TxtCurrentBalance");
        _subsPanel = this.FindControl<StackPanel>("SubscriptionsPanel");
        _searchBox = this.FindControl<TextBox>("SearchBox");

        if (_searchBox != null)
            _searchBox.TextChanged += SearchBox_TextChanged;

        var addSubBtn = this.FindControl<Button>("AddSubscriptionButton");
        if (addSubBtn != null) addSubBtn.Click += AddButton_Click;

        var addBalanceBtn = this.FindControl<Button>("AddBalanceButton");
        if (addBalanceBtn != null) addBalanceBtn.Click += AddButton1_Click;

        var showExpensesBtn = this.FindControl<Button>("ShowExpensesButton");
        if (showExpensesBtn != null) showExpensesBtn.Click += ShowExpenses_Click;

        var showHistoryBtn = this.FindControl<Button>("ShowHistoryButton");
        if (showHistoryBtn != null) showHistoryBtn.Click += ShowHistory_Click;

        var exportBtn = this.FindControl<Button>("ExportToExcelButton");
if (exportBtn != null) exportBtn.Click += ExportToExcel_Click;

        _db = new DatabaseHelper();
        LoadData();
        UpdateBalanceDisplay();

        _timer = new Timer(1000);
        _timer.Elapsed += (s, e) => Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
        {
            CheckExpiredAndCanceled();
            RefreshSubs();
        });
        _timer.Start();
    }

    private void LoadData()
    {
        _balance = _db.LoadBalance();
        _subs = _db.LoadAllSubscriptions();
        _history = _db.LoadAllHistory();
        _expenses = _db.LoadAllExpenses();
        FixDates();
        RefreshSubs();
    }

    private void FixDates()
    {
        string newSubs = "";
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in items)
        {
            string[] p = item.Split('|');
            if (p.Length >= 6)
            {
                string name = p[0];
                decimal price = decimal.Parse(p[1], CultureInfo.InvariantCulture);
                string term = p[2];
                DateTime endDate, cancelTime;
                if (!DateTime.TryParse(p[3], out endDate))
                {
                    string termLower = term.ToLower();
                    if (termLower == "день" || termLower == "1 день")
                        endDate = DateTime.Now.AddDays(1);
                    else if (termLower == "неделя" || termLower == "1 неделя")
                        endDate = DateTime.Now.AddDays(7);
                    else if (termLower == "месяц" || termLower == "1 месяц")
                        endDate = DateTime.Now.AddMonths(1);
                    else
                        endDate = DateTime.Now.AddMonths(1);
                }
                if (!DateTime.TryParse(p[5], out cancelTime))
                    cancelTime = DateTime.MinValue;
                newSubs += $"{name}|{price.ToString(CultureInfo.InvariantCulture)}|{term}|{endDate:o}|{p[4]}|{cancelTime:o};";
            }
        }
        _subs = newSubs;
        SaveAllData();
    }

    private void SaveAllData()
    {
        _db.SaveBalance(_balance);
        _db.SaveAllSubscriptions(_subs);
        _db.SaveAllHistory(_history);
        _db.SaveAllExpenses(_expenses);
    }

    private void CheckExpiredAndCanceled()
    {
        string newSubs = "";
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in items)
        {
            string[] parts = item.Split('|');
            if (parts.Length >= 6)
            {
                bool isActive = parts[4] == "True";
                DateTime endDate = DateTime.Parse(parts[3]);
                DateTime cancelTime = DateTime.Parse(parts[5]);
                if (isActive && DateTime.Now <= endDate)
                    newSubs += item + ";";
                else if (!isActive && DateTime.Now <= cancelTime.AddMinutes(1))
                    newSubs += item + ";";
            }
        }
        if (_subs != newSubs)
        {
            _subs = newSubs;
            SaveAllData();
            RefreshSubs();
        }
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e) => RefreshSubs();

    private void RefreshSubs()
    {
        if (_subsPanel == null) return;
        _subsPanel.Children.Clear();
        var wrapPanel = new WrapPanel
        {
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Avalonia.Thickness(5)
        };
        string searchText = _searchBox?.Text ?? "";
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < items.Length; i++)
        {
            string[] p = items[i].Split('|');
            if (p.Length < 6) continue;
            string name = p[0];
            if (!string.IsNullOrEmpty(searchText) && !name.ToLower().Contains(searchText.ToLower()))
                continue;
            decimal price = decimal.Parse(p[1], CultureInfo.InvariantCulture);
            string term = p[2];
            DateTime end = DateTime.Parse(p[3]);
            bool active = p[4] == "True";
            DateTime cancelTime = DateTime.Parse(p[5]);
            string subscriptionData = items[i];
            string bgColor = active ? "#E8F8F5" : "#FFF3E0";
            var border = new Border
            {
                Width = 250, Height = 180, Margin = new Avalonia.Thickness(10),
                CornerRadius = new Avalonia.CornerRadius(12), Padding = new Avalonia.Thickness(15),
                Background = new SolidColorBrush(Color.Parse(bgColor))
            };
            var stack = new StackPanel();
            string statusText = "";
            if (!active)
            {
                TimeSpan timeLeft = cancelTime.AddMinutes(1) - DateTime.Now;
                if (timeLeft.TotalSeconds > 0)
                    statusText = $"\n(удалится через {(int)timeLeft.TotalSeconds} сек)";
                else
                    statusText = "\n(удаление...)";
            }
            var nameText = new TextBlock { Text = name + statusText, FontSize = 20, FontWeight = FontWeight.Bold, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            if (!active) nameText.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            stack.Children.Add(nameText);
            string timeLeftText = GetTimeLeft(end, active);
            var infoText = new TextBlock { Text = $"💰 {price:F2} ₽ / {term}\n⏰ {timeLeftText}", Margin = new Avalonia.Thickness(0, 10, 0, 10), FontSize = 15 };
            if (!active) infoText.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            stack.Children.Add(infoText);
            var btns = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 10, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            if (active)
            {
                var extendBtn = new Button { Content = "Продлить", Width = 90, Background = new SolidColorBrush(Color.Parse("#3498DB")), Foreground = Brushes.White, BorderThickness = new Avalonia.Thickness(0), CornerRadius = new Avalonia.CornerRadius(6), Tag = subscriptionData };
                extendBtn.Click += (s, e) => ExtendSubByData((string)((Button)s).Tag);
                var cancelBtn = new Button { Content = "Отменить", Width = 90, Background = new SolidColorBrush(Color.Parse("#E74C3C")), Foreground = Brushes.White, BorderThickness = new Avalonia.Thickness(0), CornerRadius = new Avalonia.CornerRadius(6), Tag = subscriptionData };
                cancelBtn.Click += (s, e) => CancelSubByData((string)((Button)s).Tag);
                btns.Children.Add(extendBtn);
                btns.Children.Add(cancelBtn);
            }
            else
            {
                var restoreBtn = new Button { Content = "Восстановить", Width = 110, Background = new SolidColorBrush(Color.Parse("#27AE60")), Foreground = Brushes.White, BorderThickness = new Avalonia.Thickness(0), CornerRadius = new Avalonia.CornerRadius(6), Tag = subscriptionData, FontSize = 12 };
                restoreBtn.Click += (s, e) => RestoreSubscriptionByData((string)((Button)s).Tag);
                btns.Children.Add(restoreBtn);
            }
            stack.Children.Add(btns);
            border.Child = stack;
            wrapPanel.Children.Add(border);
        }
        _subsPanel.Children.Add(wrapPanel);
        if (wrapPanel.Children.Count == 0)
            _subsPanel.Children.Add(new TextBlock { Text = "Нет подписок", FontSize = 16, Foreground = new SolidColorBrush(Color.Parse("#7F8C8D")), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(50) });
    }

    private string GetTimeLeft(DateTime end, bool active)
    {
        if (!active) return "ОТМЕНЕНА";
        var left = end - DateTime.Now;
        if (left.TotalDays >= 1) return $"{(int)left.TotalDays}д {left.Hours}ч";
        if (left.TotalHours >= 1) return $"{(int)left.TotalHours}ч {left.Minutes}мин";
        if (left.TotalMinutes >= 1) return $"{(int)left.TotalMinutes}мин";
        if (left.TotalSeconds >= 1) return $"{(int)left.TotalSeconds}сек";
        return "истекла";
    }

    private void UpdateBalanceDisplay()
    {
        if (_txtCurrentBalance != null)
            _txtCurrentBalance.Text = $"Баланс: {_balance:F2} ₽";
    }

    private void ShowMsg(string msg)
    {
        var dialog = new Window
        {
            Title = "Инфо",
            Width = 300,
            Height = 120,
            Content = new TextBlock { Text = msg, Margin = new Avalonia.Thickness(20), TextWrapping = Avalonia.Media.TextWrapping.Wrap }
        };
        dialog.ShowDialog(this);
    }

    // ================== РАСХОДЫ ==================
    public string GetExpenses() => _expenses;
    public void AddExpense(string name, decimal amount, string term, string category)
    {
        string date = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        _expenses += $"{date}|{name}|{amount.ToString(CultureInfo.InvariantCulture)}|{term}|{category};";
        SaveAllData();
    }
    // ============================================

    // ========== ОПЕРАЦИИ С ПОДПИСКАМИ ==========
    public void AddSubscription(string name, decimal price, string term)
    {
        if (_balance < price) { ShowMsg($"Не хватает {price - _balance} ₽"); return; }
        _balance -= price;
        UpdateBalanceDisplay();
        DateTime end = DateTime.Now;
        string termLower = term.ToLower().Trim();
        if (termLower == "день" || termLower == "1 день" || termLower == "day")
            end = DateTime.Now.AddDays(1);
        else if (termLower == "неделя" || termLower == "1 неделя" || termLower == "week" || termLower == "7 дней")
            end = DateTime.Now.AddDays(7);
        else if (termLower == "месяц" || termLower == "1 месяц" || termLower == "month")
            end = DateTime.Now.AddMonths(1);
        else if (termLower == "год" || termLower == "1 год" || termLower == "year")
            end = DateTime.Now.AddYears(1);
        else
            end = DateTime.Now.AddMonths(1);
        _subs += $"{name}|{price.ToString(CultureInfo.InvariantCulture)}|{term}|{end:o}|True|{DateTime.MinValue:o};";
        AddExpense(name, price, term, "Подписка");
        RefreshSubs();
        SaveAllData();
        ShowMsg($"Подписка {name} до {end:dd.MM.yyyy HH:mm}");
    }

    public void AddBalanceWithDetails(decimal amount, string method, string currency)
    {
        _balance += amount;
        UpdateBalanceDisplay();
        _history += $"{DateTime.Now:dd.MM.yyyy HH:mm}|{amount.ToString("F2", CultureInfo.InvariantCulture)}|{method}|{currency};";
        SaveAllData();
        ShowMsg($"Баланс пополнен на {amount} {currency}!");
    }

    public void ExtendSubByData(string subscriptionData)
    {
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == subscriptionData)
            {
                string[] p = subscriptionData.Split('|');
                string name = p[0];
                bool active = p[4] == "True";
                decimal price = decimal.Parse(p[1], CultureInfo.InvariantCulture);
                string term = p[2];
                DateTime end = DateTime.Parse(p[3]);
                if (!active) { ShowMsg("Нельзя продлить отмененную подписку"); return; }
                if (_balance < price) { ShowMsg($"Не хватает {price - _balance} ₽"); return; }
                _balance -= price;
                UpdateBalanceDisplay();
                string termLower = term.ToLower().Trim();
                if (termLower == "день" || termLower == "1 день" || termLower == "day") end = end.AddDays(1);
                else if (termLower == "неделя" || termLower == "1 неделя" || termLower == "week" || termLower == "7 дней") end = end.AddDays(7);
                else if (termLower == "месяц" || termLower == "1 месяц" || termLower == "month") end = end.AddMonths(1);
                else if (termLower == "год" || termLower == "1 год" || termLower == "year") end = end.AddYears(1);
                else end = end.AddMonths(1);
                items[i] = $"{name}|{price.ToString(CultureInfo.InvariantCulture)}|{term}|{end:o}|True|{p[5]}";
                _subs = string.Join(";", items) + ";";
                AddExpense(name, price, term, "Продление");
                RefreshSubs();
                SaveAllData();
                ShowMsg($"🔄 Подписка {name} продлена до {end:dd.MM.yyyy HH:mm}");
                return;
            }
        }
    }

    public void CancelSubByData(string subscriptionData)
    {
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == subscriptionData)
            {
                string[] p = subscriptionData.Split('|');
                string name = p[0];
                decimal price = decimal.Parse(p[1], CultureInfo.InvariantCulture);
                string term = p[2];
                DateTime endDate = DateTime.Parse(p[3]);
                items[i] = $"{name}|{price.ToString(CultureInfo.InvariantCulture)}|{term}|{endDate:o}|False|{DateTime.Now:o}";
                _subs = string.Join(";", items) + ";";
                RefreshSubs();
                SaveAllData();
                ShowMsg($"⏸ Подписка {name} отменена, будет удалена через 1 минуту");
                return;
            }
        }
    }

    public void RestoreSubscriptionByData(string subscriptionData)
    {
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == subscriptionData)
            {
                string[] p = subscriptionData.Split('|');
                string name = p[0];
                decimal price = decimal.Parse(p[1], CultureInfo.InvariantCulture);
                string term = p[2];
                DateTime endDate = DateTime.Parse(p[3]);
                if (endDate < DateTime.Now) { ShowMsg("Нельзя восстановить истекшую подписку!"); return; }
                items[i] = $"{name}|{price.ToString(CultureInfo.InvariantCulture)}|{term}|{endDate:o}|True|{DateTime.MinValue:o}";
                _subs = string.Join(";", items) + ";";
                RefreshSubs();
                SaveAllData();
                ShowMsg($"✅ Подписка {name} восстановлена!");
                return;
            }
        }
    }




// ========== ЭКСПОРТ В EXCEL ==========
private async void ExportToExcel_Click(object? sender, RoutedEventArgs e)
{
    var dtSubscriptions = BuildSubscriptionsDataTable();
    var dtExpenses = BuildExpensesDataTable();
    var dtHistory = BuildHistoryDataTable();

    if (dtSubscriptions.Rows.Count == 0 && dtExpenses.Rows.Count == 0 && dtHistory.Rows.Count == 0)
    {
        await ShowMessageAsync("Нет данных для экспорта.");
        return;
    }

    var topLevel = TopLevel.GetTopLevel(this);
    if (topLevel == null) return;

    var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
    {
        Title = "Сохранить отчёт Excel",
        SuggestedFileName = $"Отчёт_от_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx",
        DefaultExtension = "xlsx",
        FileTypeChoices = new[] { new FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } } }
    });

    if (file == null) return;

    try
    {
        using (var workbook = new XLWorkbook())
        {
            if (dtSubscriptions.Rows.Count > 0)
                workbook.Worksheets.Add(dtSubscriptions, "Подписки");
            if (dtExpenses.Rows.Count > 0)
                workbook.Worksheets.Add(dtExpenses, "Расходы");
            if (dtHistory.Rows.Count > 0)
                workbook.Worksheets.Add(dtHistory, "Пополнения");

            foreach (var ws in workbook.Worksheets)
                ws.Columns().AdjustToContents();

            await using var stream = await file.OpenWriteAsync();
            workbook.SaveAs(stream);
        }
        await ShowMessageAsync($"✅ Данные сохранены:\n{file.Path.LocalPath}");
    }
    catch (Exception ex)
    {
        await ShowMessageAsync($"❌ Ошибка: {ex.Message}");
    }
}

private DataTable BuildSubscriptionsDataTable()
{
    var dt = new DataTable();
    dt.Columns.Add("Название", typeof(string));
    dt.Columns.Add("Цена", typeof(decimal));
    dt.Columns.Add("Срок", typeof(string));
    dt.Columns.Add("Дата окончания", typeof(DateTime));
    dt.Columns.Add("Активна", typeof(string));
    dt.Columns.Add("Отменена", typeof(string));

    var items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var item in items)
    {
        var parts = item.Split('|');
        if (parts.Length >= 6)
        {
            bool isActive = parts[4] == "True";
            bool isCanceled = parts[5] != DateTime.MinValue.ToString("o");

            var row = dt.NewRow();
            row[0] = parts[0];
            row[1] = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
            row[2] = parts[2];
            row[3] = DateTime.Parse(parts[3]);
            row[4] = isActive ? "Да" : "Нет";
            row[5] = isCanceled ? "Да" : "Нет";
            dt.Rows.Add(row);
        }
    }
    return dt;
}

private DataTable BuildExpensesDataTable()
{
    var dt = new DataTable();
    dt.Columns.Add("Дата", typeof(DateTime));
    dt.Columns.Add("Название", typeof(string));
    dt.Columns.Add("Сумма", typeof(decimal));
    dt.Columns.Add("Срок", typeof(string));
    dt.Columns.Add("Категория", typeof(string));

    var items = _expenses.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var item in items)
    {
        var parts = item.Split('|');
        if (parts.Length >= 5)
        {
            var row = dt.NewRow();
            row[0] = DateTime.ParseExact(parts[0], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            row[1] = parts[1];
            row[2] = decimal.Parse(parts[2], CultureInfo.InvariantCulture);
            row[3] = parts[3];
            row[4] = parts[4];
            dt.Rows.Add(row);
        }
    }
    return dt;
}

private DataTable BuildHistoryDataTable()
{
    var dt = new DataTable();
    dt.Columns.Add("Дата", typeof(DateTime));
    dt.Columns.Add("Сумма", typeof(decimal));
    dt.Columns.Add("Способ оплаты", typeof(string));
    dt.Columns.Add("Валюта", typeof(string));

    var items = _history.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var item in items)
    {
        var parts = item.Split('|');
        if (parts.Length >= 4)
        {
            var row = dt.NewRow();
            row[0] = DateTime.ParseExact(parts[0], "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
            row[1] = decimal.Parse(parts[1], CultureInfo.InvariantCulture);
            row[2] = parts[2];
            row[3] = parts[3];
            dt.Rows.Add(row);
        }
    }
    return dt;
}

private async Task ShowMessageAsync(string message)
{
    var dialog = new Window
    {
        Title = "Информация",
        Width = 350,
        Height = 150,
        Content = new TextBlock
        {
            Text = message,
            Margin = new Avalonia.Thickness(20),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        },
        WindowStartupLocation = WindowStartupLocation.CenterOwner
    };
    await dialog.ShowDialog(this);
}
    // ========== ОБРАБОТЧИКИ КНОПОК ==========
    public async void AddButton_Click(object? s, RoutedEventArgs e)
        => await new AddSubscriptionWindow(this).ShowDialog(this);

    public async void AddButton1_Click(object? s, RoutedEventArgs e)
        => await new AddBalanceWindow(this).ShowDialog(this);

    public async void ShowExpenses_Click(object? s, RoutedEventArgs e)
        => await new AddExpensesWindow(this).ShowDialog(this);

    public async void ShowHistory_Click(object? s, RoutedEventArgs e)
        => await new AddHistoryWindow(this).ShowDialog(this);
        

    public string GetSubscriptions() => _subs;
    public string GetHistory() => _history;
}