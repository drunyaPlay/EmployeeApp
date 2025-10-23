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
            Console.WriteLine("Произошла ошибка при работе с базой данных: " + ex.Message);

            if (conn.State == System.Data.ConnectionState.Open)
            {
                conn.Close();
                Console.WriteLine("Соединение с базой данных было закрыто из-за ошибки.");
            }
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
    // Обновление сотрудника
    static void UpdateEmployee()
    {
        int id = ReadInt("ID сотрудника");
        using var check = new SqlCommand("SELECT COUNT(*) FROM Employees WHERE EmployeeID=@id", conn);
        check.Parameters.AddWithValue("@id", id);
        if ((int)check.ExecuteScalar() == 0)
        {
            Console.WriteLine("Сотрудник с таким ID не найден.");
            return;
        }

        Console.WriteLine("Введите новые значения (Enter — оставить без изменений):");

        string first = ReadString("Имя");
        string last = ReadString("Фамилия");

        Console.Write("Email: ");
        string emailInput = Console.ReadLine();
        string email = string.IsNullOrWhiteSpace(emailInput) ? null : ReadEmail(emailInput);

        Console.Write("Дата рождения (ДД-ММ-ГГГГ): ");
        string dobInput = Console.ReadLine();
        DateTime? dob = null;
        if (!string.IsNullOrWhiteSpace(dobInput))
        {
            while (!DateTime.TryParseExact(dobInput, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime temp))
            {
                Console.Write("Неверный формат, повторите (ДД-ММ-ГГГГ): ");
                dobInput = Console.ReadLine();
            }
            dob = DateTime.ParseExact(dobInput, "dd-MM-yyyy", CultureInfo.InvariantCulture);
        }

        Console.Write("Зарплата: ");
        string salaryInput = Console.ReadLine();
        decimal? salary = null;
        if (!string.IsNullOrWhiteSpace(salaryInput))
        {
            while (!decimal.TryParse(salaryInput, out decimal val))
            {
                Console.Write("Введите корректное число: ");
                salaryInput = Console.ReadLine();
            }
            salary = decimal.Parse(salaryInput);
        }

        var updates = new List<string>();
        var cmd = new SqlCommand();
        cmd.Connection = conn;

        if (!string.IsNullOrWhiteSpace(first)) { updates.Add("FirstName=@f"); cmd.Parameters.AddWithValue("@f", first); }
        if (!string.IsNullOrWhiteSpace(last)) { updates.Add("LastName=@l"); cmd.Parameters.AddWithValue("@l", last); }
        if (email != null) { updates.Add("Email=@e"); cmd.Parameters.AddWithValue("@e", email); }
        if (dob.HasValue) { updates.Add("DateOfBirth=@d"); cmd.Parameters.AddWithValue("@d", dob.Value); }
        if (salary.HasValue) { updates.Add("Salary=@s"); cmd.Parameters.AddWithValue("@s", salary.Value); }

        if (updates.Count == 0)
        {
            Console.WriteLine("Нет изменений для обновления.");
            return;
        }

        cmd.CommandText = $"UPDATE Employees SET {string.Join(", ", updates)} WHERE EmployeeID=@id";
        cmd.Parameters.AddWithValue("@id", id);
        SafeExecuteNonQuery(cmd, "Данные обновлены.");
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