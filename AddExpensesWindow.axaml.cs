using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.Globalization;  
namespace MySubs;

public partial class AddExpensesWindow : Window
{
    private MainWindow _mainWindow;
    
    public AddExpensesWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        
        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null) closeButton.Click += (s, e) => Close();
        
        var listExpenses = this.FindControl<ListBox>("ListExpenses");
        var totalText = this.FindControl<TextBlock>("TxtTotalExpenses");
        
        LoadExpensesFromDatabase(listExpenses, totalText);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    
    private void LoadExpensesFromDatabase(ListBox? listExpenses, TextBlock? totalText)
    {
        if (listExpenses == null) return;
        
        listExpenses.Items?.Clear();
        
        string expensesData = _mainWindow.GetExpenses();
        string[] items = expensesData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        var expenses = new ObservableCollection<string>();
        decimal total = 0;
        
        for (int i = items.Length - 1; i >= 0; i--)
        {
            string[] p = items[i].Split('|');
            if (p.Length >= 5)
            {
                string date = p[0];
                string name = p[1];
                // Используем InvariantCulture (точка как разделитель) – так сохраняются числа
                if (decimal.TryParse(p[2], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    string term = p[3];
                    string category = p[4];
                    total += amount;
                    string expenseText = $"📅 {date} | {name} | -{amount:F2} ₽ / {term} | {category}";
                    expenses.Add(expenseText);
                }
                else
                {
                    // Отладка: если парсинг не удался, покажем проблемную строку
                    expenses.Add($"⚠ Ошибка парсинга: {items[i]}");
                }
            }
        }
        
        if (expenses.Count == 0)
            expenses.Add("📭 Нет записей о расходах");
        
        listExpenses.ItemsSource = expenses;
        
        if (totalText != null)
        {
            totalText.Text = $"💰 Всего потрачено: {total:F2} ₽";
            totalText.Foreground = total > 5000 ? new SolidColorBrush(Color.Parse("#E74C3C")) :
                                   total > 2000 ? new SolidColorBrush(Color.Parse("#F39C12")) :
                                   new SolidColorBrush(Color.Parse("#27AE60"));
        }
    }
}