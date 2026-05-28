using Microsoft.Data.Sqlite;    // Подключает SQLite - легкую встроенную базу данных
using System;                    // Подключает базовые типы (DateTime, String и т.д.)

namespace MySubs;                // Пространство имен (как у всех классов)

// Класс для работы с базой данных
// public - доступен из любого места
public class DatabaseHelper
{
    // Строка подключения к БД (хранит путь к файлу)
   
    private string _connectionString;
    
    // Конструктор - вызывается при создании объекта
    public DatabaseHelper()
    {
        //определяем подключение
        _connectionString = $"Data Source=mysubs.db";
        
        // Создает таблицы в базе данных (если их нет)
        CreateTables();
    }
    
    // Создает таблицы в базе данных
   // В методе CreateTables() добавьте новую таблицу:
private void CreateTables()
{
    using var connection = new SqliteConnection(_connectionString);
    connection.Open();
    
    string sql = @"
        -- Таблица для хранения баланса
        CREATE TABLE IF NOT EXISTS Balance (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Amount REAL NOT NULL
        );
        
        -- Таблица для хранения подписок
        CREATE TABLE IF NOT EXISTS Subscriptions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Price REAL NOT NULL,
            Term TEXT NOT NULL,
            EndDate TEXT NOT NULL,
            IsActive INTEGER NOT NULL,
            CancelTime TEXT
        );
        
        -- Таблица для хранения истории пополнений
        CREATE TABLE IF NOT EXISTS History (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Date TEXT NOT NULL,
            Amount REAL NOT NULL,
            PaymentMethod TEXT NOT NULL,
            Currency TEXT NOT NULL
        );
        
        -- НОВАЯ ТАБЛИЦА ДЛЯ РАСХОДОВ
        CREATE TABLE IF NOT EXISTS Expenses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Date TEXT NOT NULL,
            Name TEXT NOT NULL,
            Amount REAL NOT NULL,
            Term TEXT NOT NULL,
            Category TEXT NOT NULL
        );
    ";
    
    using var command = new SqliteCommand(sql, connection);
    command.ExecuteNonQuery();
}

// НОВЫЙ МЕТОД - СОХРАНЯЕТ РАСХОД
public void SaveExpense(string date, string name, decimal amount, string term, string category)
{
    using var connection = new SqliteConnection(_connectionString);
    connection.Open();
    
    string sql = @"INSERT INTO Expenses (Date, Name, Amount, Term, Category) 
                   VALUES (@date, @name, @amount, @term, @category)";
    
    using var cmd = new SqliteCommand(sql, connection);
    cmd.Parameters.AddWithValue("@date", date);
    cmd.Parameters.AddWithValue("@name", name);
    cmd.Parameters.AddWithValue("@amount", amount);
    cmd.Parameters.AddWithValue("@term", term);
    cmd.Parameters.AddWithValue("@category", category);
    
    cmd.ExecuteNonQuery();
}

// НОВЫЙ МЕТОД - ЗАГРУЖАЕТ ВСЕ РАСХОДЫ
public string LoadAllExpenses()
{
    using var connection = new SqliteConnection(_connectionString);
    connection.Open();
    
    using var cmd = new SqliteCommand("SELECT Date, Name, Amount, Term, Category FROM Expenses ORDER BY Id DESC", connection);
    using var reader = cmd.ExecuteReader();
    
    string result = "";
    while (reader.Read())
    {
        result += $"{reader.GetString(0)}|{reader.GetString(1)}|{reader.GetDecimal(2)}|{reader.GetString(3)}|{reader.GetString(4)};";
    }
    
    return result;
}

// НОВЫЙ МЕТОД - ОЧИЩАЕТ ТАБЛИЦУ РАСХОДОВ
public void ClearExpenses()
{
    using var connection = new SqliteConnection(_connectionString);
    connection.Open();
    
    using var cmd = new SqliteCommand("DELETE FROM Expenses", connection);
    cmd.ExecuteNonQuery();
}

// НОВЫЙ МЕТОД - СОХРАНЯЕТ ВСЕ РАСХОДЫ (для синхронизации)
public void SaveAllExpenses(string expensesData)
{
    using var connection = new SqliteConnection(_connectionString);
    connection.Open();
    
    // Очищаем старые расходы
    using var clearCmd = new SqliteCommand("DELETE FROM Expenses", connection);
    clearCmd.ExecuteNonQuery();
    
    // Разбираем строку расходов
    string[] items = expensesData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
    
    foreach (string item in items)
    {
        string[] p = item.Split('|');
        if (p.Length >= 5)
        {
            SaveExpense(p[0], p[1], decimal.Parse(p[2]), p[3], p[4]);
        }
    }
}
    // Сохраняет баланс в БД
    public void SaveBalance(decimal amount)
    {
        // Открывает соединение
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Сначала удаляет ВСЕ старые записи о балансе
        // DELETE FROM Balance - удалить всё из таблицы Balance
        using var deleteCmd = new SqliteCommand("DELETE FROM Balance", connection);
        deleteCmd.ExecuteNonQuery();  // Выполняет удаление
        
        // Затем вставляет новую запись с текущим балансом
        // INSERT INTO Balance (Amount) VALUES (@amount) - вставить новую запись
        using var insertCmd = new SqliteCommand("INSERT INTO Balance (Amount) VALUES (@amount)", connection);
        
        insertCmd.Parameters.AddWithValue("@amount", amount);
        
        insertCmd.ExecuteNonQuery();  // Выполняет вставку
    }
    
    // Загружает баланс из БД
    public decimal LoadBalance()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // SELECT Amount FROM Balance - выбрать колонку Amount из таблицы Balance
        // ORDER BY Id DESC - сортировать по Id в обратном порядке (сначала новые)
        // LIMIT 1 - взять только первую запись (самую новую)
        using var cmd = new SqliteCommand("SELECT Amount FROM Balance ORDER BY Id DESC LIMIT 1", connection);
        
        // Выполняет запрос и возвращает результат
        // ExecuteScalar() - возвращает одно значение
        var result = cmd.ExecuteScalar();
        
        // Если результат есть - преобразует в decimal, иначе возвращает 0
        // ? : - тернарный оператор (если result не null то..., иначе 0)
        return result != null ? Convert.ToDecimal(result) : 0;
    }
    
    // Сохраняет одну подписку в БД
    public void SaveSubscription(string name, decimal price, string term, DateTime endDate, bool isActive, DateTime? cancelTime)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // SQL запрос на вставку подписки
        string sql = @"INSERT INTO Subscriptions (Name, Price, Term, EndDate, IsActive, CancelTime) 
                       VALUES (@name, @price, @term, @endDate, @isActive, @cancelTime)";
        
        using var cmd = new SqliteCommand(sql, connection);
        
        // Добавляет параметры (защита от SQL инъекций)
        cmd.Parameters.AddWithValue("@name", name);           // Название
        cmd.Parameters.AddWithValue("@price", price);         // Цена
        cmd.Parameters.AddWithValue("@term", term);           // Срок
        // Преобразует дату в строку формата "2024-01-15 14:30:00"
        cmd.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
        // Преобразует bool в 1 или 0 (SQLite не умеет bool)
        cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
        // Если есть время отмены - преобразует в строку, иначе пустая строка
        cmd.Parameters.AddWithValue("@cancelTime", cancelTime.HasValue ? cancelTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "");
        
        cmd.ExecuteNonQuery();  // Выполняет вставку
    }
    
    // Сохраняет ВСЕ подписки (сначала очищает таблицу, потом вставляет заново)
    public void SaveAllSubscriptions(string subsData)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Очищает таблицу подписок (удаляет всё)
        using var clearCmd = new SqliteCommand("DELETE FROM Subscriptions", connection);
        clearCmd.ExecuteNonQuery();
        
        // Разбирает строку с подписками на отдельные записи
        // Разделитель: ; (Netflix|599|месяц|...;Spotify|199...)
        string[] items = subsData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Проходит по каждой подписке
        foreach (string item in items)
        {
            // Разбивает подписку на части (разделитель |)
            // ["Netflix", "599", "месяц", "2024-...", "True", "0001-..."]
            string[] p = item.Split('|');
            
            if (p.Length >= 6)  // Проверяет что подписка содержит все 6 частей
            {
                // Извлекает данные из строки
                string name = p[0];                      // Название
                decimal price = decimal.Parse(p[1]);     // Цена
                string term = p[2];                      // Срок
                DateTime endDate = DateTime.Parse(p[3]); // Дата окончания
                bool isActive = p[4] == "True";          // Активна?
                // Время отмены (если 0001... значит нет)
                DateTime? cancelTime = p[5] != "0001-01-01T00:00:00" ? DateTime.Parse(p[5]) : null;
                
                // Сохраняет одну подписку
                SaveSubscription(name, price, term, endDate, isActive, cancelTime);
            }
        }
    }
    
    // Загружает ВСЕ подписки из БД
    public string LoadAllSubscriptions()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Выбирает все колонки из таблицы Subscriptions (без Id)
        using var cmd = new SqliteCommand("SELECT Name, Price, Term, EndDate, IsActive, CancelTime FROM Subscriptions", connection);
        
        // Выполняет запрос и получает читатель данных
        using var reader = cmd.ExecuteReader();
        
        string result = "";  // Строка для сбора результата
        
        // Читает каждую строку результата
        // Read() возвращает true пока есть строки
        while (reader.Read())
        {
            // Читает каждую колонку
            string name = reader.GetString(0);        // Колонка 0 - Name
            decimal price = reader.GetDecimal(1);     // Колонка 1 - Price
            string term = reader.GetString(2);        // Колонка 2 - Term
            DateTime endDate = DateTime.Parse(reader.GetString(3));  // Колонка 3 - EndDate
            bool isActive = reader.GetInt32(4) == 1;  // Колонка 4 - IsActive (1/0 в True/False)
            
            // Читает время отмены (может быть NULL)
            string cancelTimeStr = reader.IsDBNull(5) ? "" : reader.GetString(5);
            DateTime cancelTime = !string.IsNullOrEmpty(cancelTimeStr) ? DateTime.Parse(cancelTimeStr) : DateTime.MinValue;
            
            // Собирает строку в формате: "Netflix|599|месяц|2024...|True|0001...;"
            result += $"{name}|{price}|{term}|{endDate}|{isActive}|{cancelTime};";
        }
        
        return result;  // Возвращает строку со всеми подписками
    }
    
    // Сохраняет одну запись в историю
    public void SaveHistory(string date, decimal amount, string method, string currency)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        string sql = @"INSERT INTO History (Date, Amount, PaymentMethod, Currency) 
                       VALUES (@date, @amount, @method, @currency)";
        
        using var cmd = new SqliteCommand(sql, connection);
        
        // Добавляет параметры
        cmd.Parameters.AddWithValue("@date", date);        // Дата
        cmd.Parameters.AddWithValue("@amount", amount);    // Сумма
        cmd.Parameters.AddWithValue("@method", method);    // Способ оплаты
        cmd.Parameters.AddWithValue("@currency", currency);// Валюта
        
        cmd.ExecuteNonQuery();  // Выполняет вставку
    }
    
    // Сохраняет ВСЮ историю
    public void SaveAllHistory(string historyData)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Очищает таблицу истории
        using var clearCmd = new SqliteCommand("DELETE FROM History", connection);
        clearCmd.ExecuteNonQuery();
        
        // Разбирает строку истории на записи (разделитель ;)
        string[] items = historyData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Проходит по каждой записи
        foreach (string item in items)
        {
            // Разбивает запись на части (разделитель |)
            string[] p = item.Split('|');
            
            if (p.Length >= 4)  // Проверяет что есть все 4 части
            {
                // Сохраняет одну запись
                SaveHistory(p[0], decimal.Parse(p[1]), p[2], p[3]);
            }
        }
    }
    
    // Загружает ВСЮ историю из БД
    public string LoadAllHistory()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        // Выбирает всё из таблицы History, сортирует по Id (по возрастанию)
        using var cmd = new SqliteCommand("SELECT Date, Amount, PaymentMethod, Currency FROM History ORDER BY Id", connection);
        using var reader = cmd.ExecuteReader();
        
        string result = "";
        
        // Читает каждую строку
        while (reader.Read())
        {
            // Собирает строку в формате: "25.12.2024|500|Карта|₽;"
            result += $"{reader.GetString(0)}|{reader.GetDecimal(1)}|{reader.GetString(2)}|{reader.GetString(3)};";
        }
        
        return result;  // Возвращает строку с историей
    }
}