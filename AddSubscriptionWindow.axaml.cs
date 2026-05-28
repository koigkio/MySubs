using Avalonia;                    // Подключает основные классы Avalonia (Window, Application и т.д.)
using Avalonia.Controls;           // Подключает элементы управления (Button, TextBox, Window)
using Avalonia.Interactivity;      // Подключает события (RoutedEventArgs для кликов)
using Avalonia.Markup.Xaml;        // Подключает загрузчик XAML разметки

namespace MySubs;                  // Объявляет пространство имен (такое же как у MainWindow)

// Объявляет частичный класс окна добавления подписки
// public - доступен из любого места
// partial - часть класса, вторая часть в .axaml файле
// : Window - наследуется от класса Window (это окно)
public partial class AddSubscriptionWindow : Window
{
    // Поле для хранения ссылки на главное окно
    // private - доступно только внутри этого класса
    // MainWindow - тип данных (главное окно)
    // _mainWindow - имя поля (с подчеркиванием в начале - стандарт C#)
    private MainWindow _mainWindow;
    
    // Конструктор - вызывается при создании окна
    // public - доступен из любого места
    // AddSubscriptionWindow - имя конструктора совпадает с именем класса
    // (MainWindow mainWindow) - принимает один параметр - главное окно
    public AddSubscriptionWindow(MainWindow mainWindow)
    {
        InitializeComponent();     // Загружает XAML разметку (кнопки, поля из .axaml файла)
        
        _mainWindow = mainWindow;  // Сохраняет ссылку на главное окно в поле _mainWindow
        
        // Ищет на форме кнопку с именем "SaveButton"
        // this - текущее окно
        // FindControl<Button> - ищет элемент управления типа Button
        // ("SaveButton") - имя элемента в XAML
        var saveButton = this.FindControl<Button>("SaveButton");
        
        // Ищет кнопку "CancelButton"
        var cancelButton = this.FindControl<Button>("CancelButton");

        // Если кнопка сохранения найдена (не равна null)
        if (saveButton != null) 
            // Подписывается на событие Click (нажатие кнопки)
            // += - добавить обработчик
            // SaveButton_Click - имя метода, который вызовется при клике
            saveButton.Click += SaveButton_Click;
        
        // Если кнопка отмены найдена
        if (cancelButton != null) 
            cancelButton.Click += CancelButton_Click;  // Подписывается на событие клика
    }

    // Метод загрузки XAML разметки
    // private - только для внутреннего использования
    // void - ничего не возвращает
    private void InitializeComponent()
    {
        // Загружает XAML из файла .axaml с таким же именем
        // AvaloniaXamlLoader - класс для загрузки XAML
        // .Load(this) - загружает разметку в текущее окно (this)
        AvaloniaXamlLoader.Load(this);
    }

    // Обработчик нажатия на кнопку "Сохранить"
    // private - только внутри класса
    // void - ничего не возвращает
    // object? sender - отправитель (кнопка, на которую нажали)
    // RoutedEventArgs e - аргументы события (дополнительная информация)
    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // Ищет текстовое поле для ввода названия
        // FindControl<TextBox> - ищет элемент типа TextBox
        // ("NameInput") - имя элемента из XAML
        var nameInput = this.FindControl<TextBox>("NameInput");
        
        // Ищет поле для ввода цены
        var priceInput = this.FindControl<TextBox>("PriceInput");
        
        // Ищет поле для ввода срока
        var termInput = this.FindControl<TextBox>("TermInput");

        // Получает текст из поля названия
        // nameInput?.Text - если nameInput не null, берет Text, иначе null
        // ?? "" - если получили null, подставляет пустую строку
        string name = nameInput?.Text ?? "";
        
        // Получает текст из поля цены
        string priceText = priceInput?.Text ?? "";
        
        // Получает текст из поля срока
        string term = termInput?.Text ?? "";

        // Проверяет, что название не пустое
        // string.IsNullOrWhiteSpace - проверяет: null, пусто, или только пробелы
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Введите название подписки!");  // Показывает ошибку
            return;                                   // Выходит из метода (не сохраняет)
        }
        
        // Пытается преобразовать текст цены в число
        // decimal.TryParse - пробует преобразовать строку в decimal
        // priceText - входящая строка
        // out decimal price - результат преобразования (если успешно)
        // ! - отрицание (если НЕ удалось преобразовать)
        if (!decimal.TryParse(priceText, out decimal price))
        {
            ShowError("Введите корректную стоимость!");  // Ошибка: не число
            return;                                      // Выход
        }
        
        // Проверяет, что цена больше 0
        if (price <= 0)
        {
            ShowError("Стоимость должна быть больше 0!");
            return;
        }
        
        // Проверяет, что срок не пустой
        if (string.IsNullOrWhiteSpace(term))
        {
            ShowError("Введите срок (День, Неделя, Месяц или Год)!");
            return;
        }
        
        // ВСЕ ПРОВЕРКИ ПРОЙДЕНЫ - сохраняем подписку
        
        // Вызывает метод AddSubscription в главном окне
        // _mainWindow - ссылка на главное окно
        // .AddSubscription(name, price, term) - передает название, цену и срок
        _mainWindow.AddSubscription(name, price, term);
        
        // Закрывает текущее окно (окно добавления подписки)
        // Close() - закрывает окно
        Close();
    }
    
    // Метод для показа сообщения об ошибке
    // private - только внутри класса
    // void - ничего не возвращает
    // string message - текст ошибки, который нужно показать
    private void ShowError(string message)
    {
        // Создает новое окно для сообщения
        var dialog = new Window
        {
            Title = "Ошибка",              // Заголовок окна
            Width = 250,                   // Ширина 250 пикселей
            Height = 100,                  // Высота 100 пикселей
            Content = new TextBlock        // Содержимое окна - текст
            { 
                Text = message,            // Текст ошибки
                Margin = new Avalonia.Thickness(20)  // Отступы 20px со всех сторон
            }
        };
        
        // Показывает окно и ждет его закрытия
        // dialog.ShowDialog(this) - показывает диалог поверх текущего окна
        // this - текущее окно (владелец диалога)
        dialog.ShowDialog(this);
    }

    // Обработчик нажатия на кнопку "Отмена"
    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();  // Просто закрывает окно без сохранения
    }
}