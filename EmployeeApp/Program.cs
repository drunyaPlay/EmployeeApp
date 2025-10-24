using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using ConsoleTables;

class Employee
{
    public int? Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal? Salary { get; set; }
}
class Program
{
    static SqlConnection conn = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=EmployeeDB;Trusted_Connection=True;");
    static readonly Regex EmailRegex = new Regex(@"^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
        var emp = new Employee
        {
            FirstName = ReadString("Имя"),
            LastName = ReadString("Фамилия"),
            Email = ReadEmail(),
            DateOfBirth = ReadDate("Дата рождения (ДД-ММ-ГГГГ)"),
            Salary = ReadDecimal("Зарплата")
        };

        string sql = "INSERT INTO Employees (FirstName, LastName, Email, DateOfBirth, Salary) VALUES (@f,@l,@e,@d,@s)";
        using (var cmd = new SqlCommand(sql, conn))
        {
            cmd.Parameters.AddWithValue("@f", emp.FirstName);
            cmd.Parameters.AddWithValue("@l", emp.LastName);
            cmd.Parameters.AddWithValue("@e", emp.Email);
            cmd.Parameters.AddWithValue("@d", emp.DateOfBirth);
            cmd.Parameters.AddWithValue("@s", emp.Salary);
            SafeExecuteNonQuery(cmd, "Сотрудник добавлен.");
        }
    }
    // Показ всех сотрудников
    static void ShowAll()
    {
        try
        {
            using (var cmd = new SqlCommand("SELECT * FROM Employees", conn))
            using (var reader = cmd.ExecuteReader())
            {
                var table = new ConsoleTable("ID", "Имя", "Фамилия", "Email", "ДатаРожд", "Зарплата");
                while (reader.Read())
                {
                    DateTime dob = Convert.ToDateTime(reader["DateOfBirth"]);
                    table.AddRow(
                        reader["EmployeeID"],
                        reader["FirstName"],
                        reader["LastName"],
                        reader["Email"],
                        dob.ToString("dd-MM-yyyy"),
                        reader["Salary"]
                    );
                }
                Console.WriteLine();
                table.Write(Format.Alternative);
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
        var emp = new Employee { Id = id };

        using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Employees WHERE EmployeeID=@id", conn))
        {
            checkCmd.Parameters.AddWithValue("@id", id);
            int exists = (int)checkCmd.ExecuteScalar();
            if (exists == 0)
            {
                Console.WriteLine($"Сотрудник с ID {id} не найден.");
                return;
            }
        }

        Console.WriteLine("Оставьте поле пустым, если не нужно обновлять.");
        emp.FirstName = ReadString("Имя", true);
        emp.LastName = ReadString("Фамилия", true);
        emp.Email = ReadEmail(true);
        emp.DateOfBirth = ReadDate("Дата рождения (ДД-ММ-ГГГГ)", true);
        emp.Salary = ReadDecimal("Зарплата", true);

        string sql = "UPDATE Employees SET ";
        var updates = new System.Collections.Generic.List<string>();
        using (var cmd = new SqlCommand())
        {
            cmd.Connection = conn;
            if (!string.IsNullOrEmpty(emp.FirstName)) { updates.Add("FirstName=@f"); cmd.Parameters.AddWithValue("@f", emp.FirstName); }
            if (!string.IsNullOrEmpty(emp.LastName)) { updates.Add("LastName=@l"); cmd.Parameters.AddWithValue("@l", emp.LastName); }
            if (!string.IsNullOrEmpty(emp.Email)) { updates.Add("Email=@e"); cmd.Parameters.AddWithValue("@e", emp.Email); }
            if (emp.DateOfBirth.HasValue) { updates.Add("DateOfBirth=@d"); cmd.Parameters.AddWithValue("@d", emp.DateOfBirth); }
            if (emp.Salary.HasValue) { updates.Add("Salary=@s"); cmd.Parameters.AddWithValue("@s", emp.Salary); }

            if (updates.Count == 0)
            {
                Console.WriteLine("Нет данных для обновления.");
                return;
            }

            sql += string.Join(", ", updates) + " WHERE EmployeeID=@id";
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("@id", emp.Id);
            SafeExecuteNonQuery(cmd, "Сотрудник обновлён.");
        }
    }
    // Удаление сотрудника
    static void DeleteEmployee()
    {
        int id = ReadInt("ID сотрудника для удаления");
        using (var cmd = new SqlCommand("DELETE FROM Employees WHERE EmployeeID=@id", conn))
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
                Console.WriteLine($"Средняя зарплата: {avg}, сотрудников выше средней: {count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка при подсчёте: " + ex.Message);
        }
    }
    // Вспомогательные функции
    static string ReadString(string label, bool acceptNull = false)
    {
        Console.Write($"{label}: ");
        string input = Console.ReadLine()?.Trim();
        if (acceptNull && string.IsNullOrEmpty(input)) return null;
        while (string.IsNullOrEmpty(input))
        {
            Console.Write($"{label} не может быть пустым. Повторите: ");
            input = Console.ReadLine()?.Trim();
        }
        return input;
    }

    static string ReadEmail(bool acceptNull = false)
    {
        string email = ReadString("Email", acceptNull);
        if (acceptNull && string.IsNullOrEmpty(email)) return null;

        while (!EmailRegex.IsMatch(email))
        {
            Console.Write("Некорректный Email. Повторите: ");
            email = Console.ReadLine();
            if (acceptNull && string.IsNullOrEmpty(email)) return null;
        }
        return email;
    }

    static DateTime? ReadDate(string label, bool acceptNull = false)
    {
        Console.Write($"{label}: ");
        string input = Console.ReadLine();
        if (acceptNull && string.IsNullOrEmpty(input)) return null;

        while (!DateTime.TryParseExact(input, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            Console.Write("Неверный формат. Повторите (ДД-ММ-ГГГГ): ");
            input = Console.ReadLine();
            if (acceptNull && string.IsNullOrEmpty(input)) return null;
        }
        return DateTime.ParseExact(input, "dd-MM-yyyy", CultureInfo.InvariantCulture);
    }

    static decimal? ReadDecimal(string label, bool acceptNull = false)
    {
        Console.Write($"{label}: ");
        string input = Console.ReadLine();
        if (acceptNull && string.IsNullOrEmpty(input)) return null;

        while (!decimal.TryParse(input, out decimal value))
        {
            Console.Write("Введите число: ");
            input = Console.ReadLine();
            if (acceptNull && string.IsNullOrEmpty(input)) return null;
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
