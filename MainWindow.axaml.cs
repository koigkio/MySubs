using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using Avalonia.Media;
using System.Timers;

namespace MySubs;

public partial class MainWindow : Window
{
    // Поля класса
    private decimal _balance = 0;
    private string _subs = "";
    private string _history = "";
    private StackPanel? _subsPanel;
    private TextBlock? _txtCurrentBalance;
    private TextBox? _searchBox;
    private Timer _timer;
    private DatabaseHelper _db;

    public MainWindow()
    {
        InitializeComponent();

        // Находим элементы
        _txtCurrentBalance = this.FindControl<TextBlock>("TxtCurrentBalance");
        _subsPanel = this.FindControl<StackPanel>("SubscriptionsPanel");
        _searchBox = this.FindControl<TextBox>("SearchBox");

        // Подписываемся на поиск
        if (_searchBox != null)
            _searchBox.TextChanged += SearchBox_TextChanged;

        // Кнопки
        var addSubBtn = this.FindControl<Button>("AddSubscriptionButton");
        if (addSubBtn != null) addSubBtn.Click += AddButton_Click;

        var addBalanceBtn = this.FindControl<Button>("AddBalanceButton");
        if (addBalanceBtn != null) addBalanceBtn.Click += AddButton1_Click;

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
                decimal price = decimal.Parse(p[1]);
                string term = p[2];
                DateTime endDate;
                DateTime cancelTime;

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
                {
                    cancelTime = DateTime.MinValue;
                }

                newSubs += $"{name}|{price}|{term}|{endDate:o}|{p[4]}|{cancelTime:o};";
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

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshSubs(); // При поиске просто обновляем карточки
    }

    // ГЛАВНЫЙ МЕТОД - СОЗДАЕТ КАРТОЧКИ ПРОГРАММНО
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
        
        // Фильтр по поиску
        if (!string.IsNullOrEmpty(searchText) && !name.ToLower().Contains(searchText.ToLower()))
            continue;
        
        decimal price = decimal.Parse(p[1]);
        string term = p[2];
        DateTime end = DateTime.Parse(p[3]);
        bool active = p[4] == "True";
        DateTime cancelTime = DateTime.Parse(p[5]);
        
        // ВАЖНО: сохраняем ПОЛНУЮ строку подписки, а не индекс!
        string subscriptionData = items[i];
        
        // Цвет фона карточки
        string bgColor = active ? "#E8F8F5" : "#FFF3E0";
        
        // СОЗДАЕМ КАРТОЧКУ
        var border = new Border
        {
            Width = 250,
            Height = 180,
            Margin = new Avalonia.Thickness(10),
            CornerRadius = new Avalonia.CornerRadius(12),
            Padding = new Avalonia.Thickness(15),
            Background = new SolidColorBrush(Color.Parse(bgColor))
        };
        
        var stack = new StackPanel();
        
        // НАЗВАНИЕ
        string statusText = "";
        if (!active)
        {
            TimeSpan timeLeft = cancelTime.AddMinutes(1) - DateTime.Now;
            if (timeLeft.TotalSeconds > 0)
                statusText = $"\n(удалится через {(int)timeLeft.TotalSeconds} сек)";
            else
                statusText = "\n(удаление...)";
        }
        
        var nameText = new TextBlock
        {
            Text = name + statusText,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        if (!active) nameText.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
        stack.Children.Add(nameText);
        
        // ЦЕНА И ВРЕМЯ
        string timeLeftText = GetTimeLeft(end, active);
        var infoText = new TextBlock
        {
            Text = $"💰 {price:F2} ₽ / {term}\n⏰ {timeLeftText}",
            Margin = new Avalonia.Thickness(0, 10, 0, 10),
            FontSize = 15
        };
        if (!active) infoText.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
        stack.Children.Add(infoText);
        
        // КНОПКИ
        var btns = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        
        if (active)
        {
            // АКТИВНАЯ ПОДПИСКА
            var extendBtn = new Button
            {
                Content = "Продлить",
                Width = 90,
                Background = new SolidColorBrush(Color.Parse("#3498DB")),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(6),
                Tag = subscriptionData  // Передаем ВСЮ строку подписки
            };
            extendBtn.Click += (s, e) => ExtendSubByData((string)((Button)s).Tag);
            
            var cancelBtn = new Button
            {
                Content = "Отменить",
                Width = 90,
                Background = new SolidColorBrush(Color.Parse("#E74C3C")),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(6),
                Tag = subscriptionData  // Передаем ВСЮ строку подписки
            };
            cancelBtn.Click += (s, e) => CancelSubByData((string)((Button)s).Tag);
            
            btns.Children.Add(extendBtn);
            btns.Children.Add(cancelBtn);
        }
        else
        {
            // ОТМЕНЕННАЯ ПОДПИСКА
            var restoreBtn = new Button
            {
                Content = "Восстановить",
                Width = 110,
                Background = new SolidColorBrush(Color.Parse("#27AE60")),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(6),
                Tag = subscriptionData,  // Передаем ВСЮ строку подписки
                FontSize = 12,
              
            };
            restoreBtn.Click += (s, e) => RestoreSubscriptionByData((string)((Button)s).Tag);
            
            var deleteBtn = new Button
            {
                Content = "Удалить",
                Width = 110,
                Background = new SolidColorBrush(Color.Parse("#E74C3C")),
                Foreground = Brushes.White,
                BorderThickness = new Avalonia.Thickness(0),
                CornerRadius = new Avalonia.CornerRadius(6),
                Tag = subscriptionData,  // Передаем ВСЮ строку подписки
                FontSize = 12,
        
            };
            deleteBtn.Click += (s, e) => DeleteSubForeverByData((string)((Button)s).Tag);
            
            btns.Children.Add(restoreBtn);
            btns.Children.Add(deleteBtn);
        }
        
        stack.Children.Add(btns);
        border.Child = stack;
        wrapPanel.Children.Add(border);
    }
    
    _subsPanel.Children.Add(wrapPanel);
    
    if (wrapPanel.Children.Count == 0)
    {
        _subsPanel.Children.Add(new TextBlock
        {
            Text = "Нет подписок",
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Parse("#7F8C8D")),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Margin = new Avalonia.Thickness(50)
        });
    }
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
            Content = new TextBlock
            {
                Text = msg,
                Margin = new Avalonia.Thickness(20),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap
            }
        };
        dialog.ShowDialog(this);
    }

    public void AddBalanceWithDetails(decimal amount, string method, string currency)
    {
        _balance += amount;
        UpdateBalanceDisplay();
        _history += $"{DateTime.Now:dd.MM.yyyy HH:mm}|{amount:F2}|{method}|{currency};";
        SaveAllData();
        ShowMsg($"Баланс пополнен на {amount} {currency}!");
    }

    public void AddSubscription(string name, decimal price, string term)
    {
        if (_balance < price)
        {
            ShowMsg($"Не хватает {price - _balance} ₽");
            return;
        }

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

        _subs += $"{name}|{price}|{term}|{end:o}|True|{DateTime.MinValue:o};";

        RefreshSubs();
        SaveAllData();
        ShowMsg($"Подписка {name} до {end:dd.MM.yyyy HH:mm}");
    }

    public void ExtendSub(int index)
    {
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (index >= items.Length) return;

        string[] p = items[index].Split('|');
        bool active = p[4] == "True";
        decimal price = decimal.Parse(p[1]);

        if (!active) { ShowMsg("Нельзя продлить отмененную подписку"); return; }
        if (_balance < price) { ShowMsg($"Нужно {price} ₽"); return; }

        _balance -= price;
        UpdateBalanceDisplay();

        DateTime end = DateTime.Parse(p[3]);
        string term = p[2].ToLower().Trim();

        if (term == "день" || term == "1 день" || term == "day")
            end = end.AddDays(1);
        else if (term == "неделя" || term == "1 неделя" || term == "week" || term == "7 дней")
            end = end.AddDays(7);
        else if (term == "месяц" || term == "1 месяц" || term == "month")
            end = end.AddMonths(1);
        else if (term == "год" || term == "1 год" || term == "year")
            end = end.AddYears(1);
        else
            end = end.AddMonths(1);

        items[index] = $"{p[0]}|{p[1]}|{p[2]}|{end:o}|{p[4]}|{p[5]}";
        _subs = string.Join(";", items) + ";";

        RefreshSubs();
        SaveAllData();
        ShowMsg($"Продлено до {end:dd.MM.yyyy HH:mm}");
    }

    public void CancelSub(int index)
    {
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (index >= items.Length) return;

        string[] p = items[index].Split('|');
        items[index] = $"{p[0]}|{p[1]}|{p[2]}|{p[3]}|False|{DateTime.Now:o}";

        _subs = string.Join(";", items) + ";";
        RefreshSubs();
        SaveAllData();
        ShowMsg($"Подписка {p[0]} отменена и будет удалена через 1 минуту");
    }

    public void DeleteSubForever(int index)
    {
        string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (index >= items.Length) return;

        string[] p = items[index].Split('|');

        var newList = new System.Collections.Generic.List<string>(items);
        newList.RemoveAt(index);
        _subs = string.Join(";", newList);
        if (_subs.Length > 0 && !_subs.EndsWith(";")) _subs += ";";

        RefreshSubs();
        SaveAllData();
        ShowMsg($"Подписка {p[0]} удалена навсегда");
    }

    public void RestoreSubscription(int index)
{
    string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    if (index >= items.Length) return;
    
    string[] p = items[index].Split('|');
    string name = p[0];
    decimal price = decimal.Parse(p[1]);
    string term = p[2];
    DateTime endDate = DateTime.Parse(p[3]);
    
    if (endDate < DateTime.Now)
    {
        ShowMsg("Нельзя восстановить истекшую подписку!");
        return;
    }
    
    items[index] = $"{name}|{price}|{term}|{endDate:o}|True|{DateTime.MinValue:o}";
    _subs = string.Join(";", items) + ";";
    
    RefreshSubs();
    SaveAllData();
    ShowMsg($"Подписка {name} восстановлена!");
}
// ========== НОВЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С ПОДПИСКАМИ ==========

/// <summary>
/// Восстанавливает отмененную подписку
/// </summary>
public void RestoreSubscriptionByData(string subscriptionData)
{
    // Разбиваем строку подписок на массив
    string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    
    for (int i = 0; i < items.Length; i++)
    {
        if (items[i] == subscriptionData)
        {
            // Разбираем данные подписки
            string[] p = subscriptionData.Split('|');
            string name = p[0];
            decimal price = decimal.Parse(p[1]);
            string term = p[2];
            DateTime endDate = DateTime.Parse(p[3]);
            
            // Проверяем, не истекла ли подписка
            if (endDate < DateTime.Now)
            {
                ShowMsg("Нельзя восстановить истекшую подписку!");
                return;
            }
            
            // Восстанавливаем подписку (меняем False на True)
            items[i] = $"{name}|{price}|{term}|{endDate:o}|True|{DateTime.MinValue:o}";
            
            // Собираем обратно в строку
            _subs = string.Join(";", items) + ";";
            
            // Обновляем отображение и сохраняем
            RefreshSubs();
            SaveAllData();
            ShowMsg($"✅ Подписка {name} восстановлена!");
            return;
        }
    }
}

/// <summary>
/// Удаляет подписку навсегда
/// </summary>
public void DeleteSubForeverByData(string subscriptionData)
{
    string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    string name = "";
    
    for (int i = 0; i < items.Length; i++)
    {
        if (items[i] == subscriptionData)
        {
            string[] p = subscriptionData.Split('|');
            name = p[0];
            
            // Удаляем подписку из списка
            var list = new System.Collections.Generic.List<string>(items);
            list.RemoveAt(i);
            items = list.ToArray();
            break;
        }
    }
    
    // Собираем строку без удаленной подписки
    _subs = string.Join(";", items);
    if (!string.IsNullOrEmpty(_subs) && !_subs.EndsWith(";")) 
        _subs += ";";
    
    // Обновляем отображение и сохраняем
    RefreshSubs();
    SaveAllData();
    ShowMsg($"❌ Подписка {name} удалена навсегда");
}

/// <summary>
/// Продлевает подписку
/// </summary>
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
            decimal price = decimal.Parse(p[1]);
            string term = p[2];
            DateTime end = DateTime.Parse(p[3]);
            
            // Проверки
            if (!active)
            {
                ShowMsg("Нельзя продлить отмененную подписку");
                return;
            }
            
            if (_balance < price)
            {
                ShowMsg($"Не хватает {price - _balance} ₽");
                return;
            }
            
            // Списываем деньги
            _balance -= price;
            UpdateBalanceDisplay();
            
            // Увеличиваем дату окончания
            string termLower = term.ToLower().Trim();
            if (termLower == "день" || termLower == "1 день" || termLower == "day")
                end = end.AddDays(1);
            else if (termLower == "неделя" || termLower == "1 неделя" || termLower == "week" || termLower == "7 дней")
                end = end.AddDays(7);
            else if (termLower == "месяц" || termLower == "1 месяц" || termLower == "month")
                end = end.AddMonths(1);
            else if (termLower == "год" || termLower == "1 год" || termLower == "year")
                end = end.AddYears(1);
            else
                end = end.AddMonths(1);
            
            // Обновляем подписку
            items[i] = $"{name}|{price}|{term}|{end:o}|True|{p[5]}";
            _subs = string.Join(";", items) + ";";
            
            // Обновляем отображение
            RefreshSubs();
            SaveAllData();
            ShowMsg($"🔄 Подписка {name} продлена до {end:dd.MM.yyyy HH:mm}");
            return;
        }
    }
}

/// <summary>
/// Отменяет подписку
/// </summary>
public void CancelSubByData(string subscriptionData)
{
    string[] items = _subs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    
    for (int i = 0; i < items.Length; i++)
    {
        if (items[i] == subscriptionData)
        {
            string[] p = subscriptionData.Split('|');
            string name = p[0];
            decimal price = decimal.Parse(p[1]);
            string term = p[2];
            DateTime endDate = DateTime.Parse(p[3]);
            
            // Отменяем подписку
            items[i] = $"{name}|{price}|{term}|{endDate:o}|False|{DateTime.Now:o}";
            _subs = string.Join(";", items) + ";";
            
            // Обновляем отображение
            RefreshSubs();
            SaveAllData();
            ShowMsg($"⏸ Подписка {name} отменена, будет удалена через 1 минуту");
            return;
        }
    }
}



    public async void AddButton_Click(object? s, RoutedEventArgs e)
        => await new AddSubscriptionWindow(this).ShowDialog(this);

    public async void AddButton1_Click(object? s, RoutedEventArgs e)
        => await new AddBalanceWindow(this).ShowDialog(this);
 public string GetSubscriptions()
{
    return _subs;
}
}