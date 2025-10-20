using System;
using Microsoft.Data.SqlClient;
using System.Globalization;

class Program
{
    static string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=EmployeeDB;Trusted_Connection=True;";

    static void Main()
    {
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
                case "6": return;
            }
        }
    }

    static void AddEmployee()
    {
        Console.Write("Имя: "); string first = Console.ReadLine();
        Console.Write("Фамилия: "); string last = Console.ReadLine();
        Console.Write("Email: "); string email = Console.ReadLine();
        Console.Write("Дата рождения (ДД-ММ-ГГГГ): ");
        string dobInput = Console.ReadLine();

        if (!DateTime.TryParseExact(dobInput, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob))
        {
            Console.WriteLine("Неверный формат даты. Используйте ДД-ММ-ГГГГ");
            return;
        }

        Console.Write("Зарплата: "); decimal salary = decimal.Parse(Console.ReadLine());

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = "INSERT INTO Employees (FirstName, LastName, Email, DateOfBirth, Salary) VALUES (@f,@l,@e,@d,@s)";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@f", first);
                cmd.Parameters.AddWithValue("@l", last);
                cmd.Parameters.AddWithValue("@e", email);
                cmd.Parameters.AddWithValue("@d", dob);
                cmd.Parameters.AddWithValue("@s", salary);
                cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine("Сотрудник добавлен.");
    }

    static void ShowAll()
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = "SELECT * FROM Employees";
            using (var cmd = new SqlCommand(sql, conn))
            using (var reader = cmd.ExecuteReader())
            {
                Console.WriteLine("ID\tИмя\tФамилия\tEmail\tДатаРожд\tЗарплата");
                while (reader.Read())
                {
                    DateTime dob = Convert.ToDateTime(reader["DateOfBirth"]);
                    string dobStr = dob.ToString("dd-MM-yyyy");
                    Console.WriteLine($"{reader["EmployeeID"]}\t{reader["FirstName"]}\t{reader["LastName"]}\t{reader["Email"]}\t{dobStr}\t{reader["Salary"]}");
                }
            }
        }
    }

    static void UpdateEmployee()
    {
        Console.Write("ID сотрудника: "); int id = int.Parse(Console.ReadLine());
        Console.Write("Колонка для обновления (FirstName, LastName, Email, DateOfBirth, Salary): "); string col = Console.ReadLine();
        Console.Write("Новое значение: "); string val = Console.ReadLine();

        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = $"UPDATE Employees SET {col} = @v WHERE EmployeeID = @id";
            using (var cmd = new SqlCommand(sql, conn))
            {
                if (col == "DateOfBirth")
                {
                    if (!DateTime.TryParseExact(val, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dob))
                    {
                        Console.WriteLine("Неверный формат даты. Используйте ДД-ММ-ГГГГ");
                        return;
                    }
                    cmd.Parameters.AddWithValue("@v", dob);
                }
                else if (col == "Salary")
                {
                    cmd.Parameters.AddWithValue("@v", decimal.Parse(val));
                }
                else
                {
                    cmd.Parameters.AddWithValue("@v", val);
                }
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine("Обновлено.");
    }

    static void DeleteEmployee()
    {
        Console.Write("ID сотрудника для удаления: "); int id = int.Parse(Console.ReadLine());
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            string sql = "DELETE FROM Employees WHERE EmployeeID=@id";
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }
        Console.WriteLine("Удалено.");
    }

    static void CountAboveAverage()
    {
        using (var conn = new SqlConnection(connectionString))
        {
            conn.Open();
            decimal avg = (decimal)new SqlCommand("SELECT AVG(Salary) FROM Employees", conn).ExecuteScalar();
            int count = (int)new SqlCommand("SELECT COUNT(*) FROM Employees WHERE Salary > " + avg, conn).ExecuteScalar();
            Console.WriteLine($"Средняя зарплата: {avg}, сотрудников с выше средней: {count}");
        }
    }
}