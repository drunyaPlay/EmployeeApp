using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using ConsoleTables;

class Program
{
    static SqlConnection conn = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=EmployeeDB;Trusted_Connection=True;");
    static void Main()
    {
        try
        {
            conn.Open();
            Console.WriteLine("Соединение с базой данных установлено.");

            while (true)
            {
                Console.WriteLine("\n=== Меню управления сотрудниками ===");
                Console.WriteLine("1. Добавить сотрудника");
                Console.WriteLine("2. Посмотреть всех сотрудников");
                Console.WriteLine("3. Обновить информацию о сотруднике");
                Console.WriteLine("4. Удалить сотрудника");
                Console.WriteLine("5. Кол-во сотрудников с зарплатой выше средней");
                Console.WriteLine("6. Выйти");
                Console.Write("Выберите пункт (1–6): ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": AddEmployee(); break;
                    case "2": ShowAll(); break;
                    case "3": UpdateEmployee(); break;
                    case "4": DeleteEmployee(); break;
                    case "5": CountAboveAverage(); break;
                    case "6":
                        conn.Close();
                        Console.WriteLine("Соединение закрыто. До свидания!");
                        return;
                    default:
                        Console.WriteLine("Неверный пункт меню. Повторите ввод.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при подключении: " + ex.Message);
        }
    }
    // Добавление сотрудника
    static void AddEmployee()
    {
        string first = ReadString("Имя");
        string last = ReadString("Фамилия");
        string email = ReadEmail();
        DateTime dob = ReadDate("Дата рождения (ДД-ММ-ГГГГ)");
        decimal salary = ReadDecimal("Зарплата");

        string sql = "INSERT INTO Employees (FirstName, LastName, Email, DateOfBirth, Salary) VALUES (@f,@l,@e,@d,@s)";
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@f", first);
            cmd.Parameters.AddWithValue("@l", last);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@d", dob);
            cmd.Parameters.AddWithValue("@s", salary);
            SafeExecuteNonQuery(cmd, "Сотрудник добавлен.");
        }
    }
    // Показ всех сотрудников
    static void ShowAll()
    {
        try
        {
            string sql = "SELECT * FROM Employees";
            using (var cmd = new SqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                var table = new ConsoleTable("ID", "Имя", "Фамилия", "Email", "ДатаРожд", "Зарплата");

                while (reader.Read())
                {
                    DateTime dob = Convert.ToDateTime(reader["DateOfBirth"]);
                    string dobStr = dob.ToString("dd-MM-yyyy");

                    table.AddRow(
                        reader["EmployeeID"],
                        reader["FirstName"],
                        reader["LastName"],
                        reader["Email"],
                        dobStr,
                        reader["Salary"]
                    );
                }

                Console.WriteLine();
                table.Write(Format.Alternative); // красивый стиль
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при выводе данных: " + ex.Message);
        }
    }
    // Обновление сотрудника
    static void UpdateEmployee()
    {
        int id = ReadInt("ID сотрудника");

        string[] valid = { "FirstName", "LastName", "Email", "DateOfBirth", "Salary" };
        string col;

        while (true)
        {
            Console.Write("Поле для обновления (FirstName, LastName, Email, DateOfBirth, Salary): ");
            col = Console.ReadLine()?.Trim();

            if (Array.Exists(valid, v => v.Equals(col, StringComparison.OrdinalIgnoreCase)))
            {
                col = valid[Array.FindIndex(valid, v => v.Equals(col, StringComparison.OrdinalIgnoreCase))];
                break;
            }

            Console.WriteLine("Некорректное имя поля. Попробуйте снова.");
        }

        Console.Write("Новое значение: ");
        string val = Console.ReadLine();

        object value = val;
        if (col == "DateOfBirth")
            value = ReadDate("Дата рождения (ДД-ММ-ГГГГ)");
        else if (col == "Salary")
            value = ReadDecimal("Зарплата");
        else if (col == "Email")
            value = ReadEmail(val);

        string sql = $"UPDATE Employees SET {col} = @v WHERE EmployeeID = @id";
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@v", value);
            cmd.Parameters.AddWithValue("@id", id);
            SafeExecuteNonQuery(cmd, "Обновлено.");
        }
    }
    // Удаление сотрудника
    static void DeleteEmployee()
    {
        int id = ReadInt("ID сотрудника для удаления");
        string sql = "DELETE FROM Employees WHERE EmployeeID=@id";
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@id", id);
            SafeExecuteNonQuery(cmd, "Удалено.");
        }
    }
    // Подсчет выше средней з/п
    static void CountAboveAverage()
    {
        try
        {
            decimal avg = (decimal)new SqlCommand("SELECT AVG(Salary) FROM Employees", conn).ExecuteScalar();
            string sql = "SELECT COUNT(*) FROM Employees WHERE Salary > @avg";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@avg", avg);
                int count = (int)cmd.ExecuteScalar();
                Console.WriteLine($"Средняя зарплата: {avg}, сотрудников с выше средней: {count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при подсчёте: " + ex.Message);
        }
    }
    // Вспомогательные функции
    static string ReadString(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine()?.Trim() ?? "";
    }
    static string ReadEmail(string? input = null)
    {
        string email = input ?? ReadString("Email");
        Regex regex = new Regex(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$", RegexOptions.IgnoreCase);
        while (!regex.IsMatch(email))
        {
            Console.Write("Некорректный Email. Введите снова: ");
            email = Console.ReadLine();
        }
        return email;
    }
    static DateTime ReadDate(string label)
    {
        Console.Write($"{label}: ");
        string input = Console.ReadLine();
        while (!DateTime.TryParseExact(input, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            Console.Write("Неверный формат даты. Повторите (ДД-ММ-ГГГГ): ");
            input = Console.ReadLine();
        }
        return DateTime.ParseExact(input, "dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
    static decimal ReadDecimal(string label)
    {
        Console.Write($"{label}: ");
        string input = Console.ReadLine();
        while (!decimal.TryParse(input, out decimal value))
        {
            Console.Write("Введите число: ");
            input = Console.ReadLine();
        }
        return decimal.Parse(input);
    }
    static int ReadInt(string label)
    {
        Console.Write($"{label}: ");
        string input = Console.ReadLine();
        while (!int.TryParse(input, out int id))
        {
            Console.Write("Введите корректное число: ");
            input = Console.ReadLine();
        }
        return int.Parse(input);
    }
    static void SafeExecuteNonQuery(SqlCommand cmd, string successMessage)
    {
        try
        {
            cmd.ExecuteNonQuery();
            Console.WriteLine(successMessage);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при выполнении операции: " + ex.Message);
        }
    }
}