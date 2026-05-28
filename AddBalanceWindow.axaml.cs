using Avalonia;                    // Подключает основные классы Avalonia (Window, Application)
using Avalonia.Controls;           // Подключает элементы управления (Button, TextBox, Window)
using Avalonia.Interactivity;      // Подключает события (RoutedEventArgs для кликов)
using Avalonia.Markup.Xaml;        // Подключает загрузчик XAML разметки

namespace MySubs;                  // Пространство имен (такое же как у других окон)

// Объявляет частичный класс окна пополнения баланса
public partial class AddBalanceWindow : Window
{
    // Поле для хранения ссылки на главное окно
    private MainWindow _mainWindow;
    
    // Поле для хранения последнего способа оплаты (чтобы запомнить)
    private string _lastPaymentMethod = "";
    
    // Поле для хранения последней валюты (чтобы запомнить)
    private string _lastCurrency = "";
    
    // Конструктор - вызывается при создании окна
    public AddBalanceWindow(MainWindow mainWindow)
    {
        InitializeComponent();     // Загружает XAML разметку
        
        _mainWindow = mainWindow;  // Сохраняет ссылку на главное окно
        
        // Находит кнопку "Сохранить" на форме
        var saveButton = this.FindControl<Button>("SaveButton");
        
        // Находит кнопку "Отмена" на форме
        var cancelButton = this.FindControl<Button>("CancelButton");

        // Если кнопка сохранения найдена - подписывается на клик
        if (saveButton != null) saveButton.Click += SaveButton_Click;
        
        // Если кнопка отмены найдена - подписывается на клик
        if (cancelButton != null) cancelButton.Click += CancelButton_Click;
    }

    // Загружает XAML разметку из файла .axaml
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Обработчик нажатия кнопки "Сохранить"
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Находит поле для ввода суммы
        var summInput = this.FindControl<TextBox>("SummInput");
        
        // Находит поле для ввода способа оплаты
        var payInput = this.FindControl<TextBox>("PayInput");
        
        // Находит поле для ввода валюты
        var valuteInput = this.FindControl<TextBox>("ValuteInput");

        // Получает текст из поля суммы (если поле пустое - пустая строка)
        string summ = summInput?.Text ?? "";
        
        // Получает текст из поля способа оплаты
        string payText = payInput?.Text ?? "";
        
        // Получает текст из поля валюты
        string valute = valuteInput?.Text ?? "";

        // Пытается преобразовать строку суммы в число
        // decimal.TryParse - пробует превратить "500" в 500 (тип decimal)
        // out decimal amount - сюда запишется результат
        if (decimal.TryParse(summ, out decimal amount))
        {
            // ЕСЛИ СУММА ВВЕДЕНА КОРРЕКТНО:
            
            // Сохраняет способ оплаты
            // string.IsNullOrWhiteSpace(payText) - проверяет: пустой или только пробелы?
            // ? "Не указано" : payText - если пусто - пишем "Не указано", иначе берем то что ввел пользователь
            _lastPaymentMethod = string.IsNullOrWhiteSpace(payText) ? "Не указано" : payText;
            
            // Сохраняет валюту (если не ввели - ставим рубли)
            _lastCurrency = string.IsNullOrWhiteSpace(valute) ? "₽" : valute;
            
            // Передает данные в главное окно
            // amount - сумма пополнения
            // _lastPaymentMethod - способ оплаты
            // _lastCurrency - валюта
            _mainWindow.AddBalanceWithDetails(amount, _lastPaymentMethod, _lastCurrency);
            
            Close();  // Закрывает окно пополнения
        }
        else
        {
            // ЕСЛИ СУММА ВВЕДЕНА НЕКОРРЕКТНО (не число или пусто):
            
            // Создает окно с ошибкой
            var errorDialog = new Window
            {
                Title = "Ошибка",              // Заголовок окна
                Width = 250,                   // Ширина 250px
                Height = 100,                  // Высота 100px
                Content = new TextBlock        // Содержимое - текст
                { 
                    Text = "Введите корректную сумму!",  // Текст ошибки
                    Margin = new Avalonia.Thickness(20)   // Отступы 20px
                }
            };
            
            // Показывает окно с ошибкой
            errorDialog.ShowDialog(this);
            
            // Окно пополнения НЕ закрывается, пользователь может исправить ошибку
        }
    }

    // Обработчик нажатия кнопки "Отмена"
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();  // Просто закрывает окно без сохранения
    }
}