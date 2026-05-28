using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;

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
        
        // Загружаем расходы ИЗ БАЗЫ ДАННЫХ
        LoadExpensesFromDatabase(listExpenses, totalText);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void LoadExpensesFromDatabase(ListBox? listExpenses, TextBlock? totalText)
    {
        if (listExpenses == null) return;
        
        // Очищаем список
        listExpenses.Items?.Clear();
        
        // Получаем расходы ИЗ БАЗЫ (через главное окно)
        string expensesData = _mainWindow.GetExpenses();
        string[] items = expensesData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        var expenses = new ObservableCollection<string>();
        decimal total = 0;
        
        // Перебираем расходы в обратном порядке (новые сверху)
        for (int i = items.Length - 1; i >= 0; i--)
        {
            string[] p = items[i].Split('|');
            if (p.Length >= 5)
            {
                string date = p[0];
                string name = p[1];
                decimal amount = decimal.Parse(p[2]);
                string term = p[3];
                string category = p[4];
                
                total += amount;
                
                // Формируем строку для отображения
                string expenseText = $"📅 {date} | {name} | -{amount:F2} ₽ / {term} | {category}";
                expenses.Add(expenseText);
            }
        }
        
        // Если нет расходов
        if (expenses.Count == 0)
        {
            expenses.Add("📭 Нет записей о расходах");
        }
        
        // Привязываем список
        listExpenses.ItemsSource = expenses;
        
        // Обновляем итоговую сумму
        if (totalText != null)
        {
            totalText.Text = $"💰 Всего потрачено: {total:F2} ₽";
            
            if (total > 5000)
                totalText.Foreground = new SolidColorBrush(Color.Parse("#E74C3C"));
            else if (total > 2000)
                totalText.Foreground = new SolidColorBrush(Color.Parse("#F39C12"));
            else
                totalText.Foreground = new SolidColorBrush(Color.Parse("#27AE60"));
        }
    }
}