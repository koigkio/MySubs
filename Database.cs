using Microsoft.Data.Sqlite;
using System;
using System.Globalization;   // ← добавить

namespace MySubs;

public class DatabaseHelper
{
    private string _connectionString;

    public DatabaseHelper()
    {
        _connectionString = "Data Source=mysubs.db";
        CreateTables();
    }

    private void CreateTables()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        string sql = @"
            CREATE TABLE IF NOT EXISTS Balance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Amount REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Subscriptions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price REAL NOT NULL,
                Term TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                IsActive INTEGER NOT NULL,
                CancelTime TEXT
            );

            CREATE TABLE IF NOT EXISTS History (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Date TEXT NOT NULL,
                Amount REAL NOT NULL,
                PaymentMethod TEXT NOT NULL,
                Currency TEXT NOT NULL
            );

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

    // ==================== BALANCE ====================
    public void SaveBalance(decimal amount)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var deleteCmd = new SqliteCommand("DELETE FROM Balance", connection);
        deleteCmd.ExecuteNonQuery();

        using var insertCmd = new SqliteCommand("INSERT INTO Balance (Amount) VALUES (@amount)", connection);
        insertCmd.Parameters.AddWithValue("@amount", amount);
        insertCmd.ExecuteNonQuery();
    }

    public decimal LoadBalance()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("SELECT Amount FROM Balance ORDER BY Id DESC LIMIT 1", connection);
        var result = cmd.ExecuteScalar();
        return result != null ? Convert.ToDecimal(result) : 0;
    }

    // ==================== SUBSCRIPTIONS ====================
    public void SaveSubscription(string name, decimal price, string term, DateTime endDate, bool isActive, DateTime? cancelTime)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        string sql = @"INSERT INTO Subscriptions (Name, Price, Term, EndDate, IsActive, CancelTime) 
                       VALUES (@name, @price, @term, @endDate, @isActive, @cancelTime)";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@price", price);
        cmd.Parameters.AddWithValue("@term", term);
        cmd.Parameters.AddWithValue("@endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@cancelTime", cancelTime.HasValue ? cancelTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "");
        cmd.ExecuteNonQuery();
    }

    public void SaveAllSubscriptions(string subsData)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var clearCmd = new SqliteCommand("DELETE FROM Subscriptions", connection);
        clearCmd.ExecuteNonQuery();

        string[] items = subsData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in items)
        {
            string[] p = item.Split('|');
            if (p.Length >= 6)
            {
                string name = p[0];
                decimal price = decimal.Parse(p[1], CultureInfo.InvariantCulture);
                string term = p[2];
                DateTime endDate = DateTime.Parse(p[3]);
                bool isActive = p[4] == "True";
                DateTime? cancelTime = p[5] != "0001-01-01T00:00:00" ? DateTime.Parse(p[5]) : null;
                SaveSubscription(name, price, term, endDate, isActive, cancelTime);
            }
        }
    }

    public string LoadAllSubscriptions()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("SELECT Name, Price, Term, EndDate, IsActive, CancelTime FROM Subscriptions", connection);
        using var reader = cmd.ExecuteReader();

        string result = "";
        while (reader.Read())
        {
            string name = reader.GetString(0);
            decimal price = reader.GetDecimal(1);
            string term = reader.GetString(2);
            DateTime endDate = DateTime.Parse(reader.GetString(3));
            bool isActive = reader.GetInt32(4) == 1;
            string cancelTimeStr = reader.IsDBNull(5) ? "" : reader.GetString(5);
            DateTime cancelTime = !string.IsNullOrEmpty(cancelTimeStr) ? DateTime.Parse(cancelTimeStr) : DateTime.MinValue;
            result += $"{name}|{price.ToString(CultureInfo.InvariantCulture)}|{term}|{endDate:o}|{isActive}|{cancelTime:o};";
        }
        return result;
    }

    // ==================== HISTORY (пополнения) ====================
    public void SaveHistory(string date, decimal amount, string paymentMethod, string currency)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        string sql = @"INSERT INTO History (Date, Amount, PaymentMethod, Currency) 
                       VALUES (@date, @amount, @method, @currency)";

        using var cmd = new SqliteCommand(sql, connection);
        cmd.Parameters.AddWithValue("@date", date);
        cmd.Parameters.AddWithValue("@amount", amount);
        cmd.Parameters.AddWithValue("@method", paymentMethod);
        cmd.Parameters.AddWithValue("@currency", currency);
        cmd.ExecuteNonQuery();
    }

    public void SaveAllHistory(string historyData)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var clearCmd = new SqliteCommand("DELETE FROM History", connection);
        clearCmd.ExecuteNonQuery();

        string[] items = historyData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in items)
        {
            string[] p = item.Split('|');
            if (p.Length >= 4)
            {
                SaveHistory(p[0], decimal.Parse(p[1], CultureInfo.InvariantCulture), p[2], p[3]);
            }
        }
    }

    public string LoadAllHistory()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("SELECT Date, Amount, PaymentMethod, Currency FROM History ORDER BY Id", connection);
        using var reader = cmd.ExecuteReader();

        string result = "";
        while (reader.Read())
        {
            result += $"{reader.GetString(0)}|{reader.GetDecimal(1).ToString(CultureInfo.InvariantCulture)}|{reader.GetString(2)}|{reader.GetString(3)};";
        }
        return result;
    }

    // ==================== EXPENSES (расходы) ====================
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

    public void SaveAllExpenses(string expensesData)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var clearCmd = new SqliteCommand("DELETE FROM Expenses", connection);
        clearCmd.ExecuteNonQuery();

        string[] items = expensesData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string item in items)
        {
            string[] p = item.Split('|');
            if (p.Length >= 5)
            {
                SaveExpense(p[0], p[1], decimal.Parse(p[2], CultureInfo.InvariantCulture), p[3], p[4]);
            }
        }
    }

    public string LoadAllExpenses()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = new SqliteCommand("SELECT Date, Name, Amount, Term, Category FROM Expenses ORDER BY Id DESC", connection);
        using var reader = cmd.ExecuteReader();

        string result = "";
        while (reader.Read())
        {
            result += $"{reader.GetString(0)}|{reader.GetString(1)}|{reader.GetDecimal(2).ToString(CultureInfo.InvariantCulture)}|{reader.GetString(3)}|{reader.GetString(4)};";
        }
        return result;
    }
}