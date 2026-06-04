using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MySubs;

public partial class AddHistoryWindow : Window   // ← класс должен быть AddHistoryWindow
{
    private MainWindow _mainWindow;

    public AddHistoryWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;

        var closeButton = this.FindControl<Button>("CloseButton");
        if (closeButton != null) closeButton.Click += (s, e) => Close();

        var listHistory = this.FindControl<ListBox>("ListHistory");
        var totalText = this.FindControl<TextBlock>("TxtTotalHistory");

        LoadHistoryFromDatabase(listHistory, totalText);
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void LoadHistoryFromDatabase(ListBox? listHistory, TextBlock? totalText)
    {
        if (listHistory == null) return;

        listHistory.Items?.Clear();

        string historyData = _mainWindow.GetHistory();
        string[] items = historyData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        var history = new ObservableCollection<string>();
        decimal total = 0;

        for (int i = items.Length - 1; i >= 0; i--)
        {
            string[] p = items[i].Split('|');
            if (p.Length >= 4)
            {
                string date = p[0];
                if (decimal.TryParse(p[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    string payMethod = p[2];
                    string currency = p[3];
                    total += amount;
                    string historyText = $"📅 {date} | +{amount:F2} {currency} | {payMethod}";
                    history.Add(historyText);
                }
                else
                {
                    history.Add($"⚠ Ошибка данных: {items[i]}");
                }
            }
        }

        if (history.Count == 0)
            history.Add("📭 Нет записей о пополнениях");

        listHistory.ItemsSource = history;

        if (totalText != null)
        {
            totalText.Text = $"💰 Всего пополнено: {total:F2}";
            totalText.Foreground = new SolidColorBrush(Color.Parse("#27AE60"));
        }
    }
}