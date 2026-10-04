using System;
using System.Data.SqlClient;

namespace Sales_Inventory_Log_In
{
    public class AuthService
    {
        public bool Login(string username, string password, string role)
        {
            if (role == "Admin")
            {
                return username == "admin" && password == "admin123";
            }

            if (role == "Cashier")
            {
                return username == "cashier" && password == "cashier123";
            }

            if (role == "Inventory")
            {
                return username == "inventory" && password == "inventory123";
            }

            return false;
        }
    }

          

namespace Sales_Inventory_Log_In
    {
        public class AuthService
        {
            private readonly DatabaseConnection database;

            public AuthService()
            {
                database = new DatabaseConnection();
            }

            public bool Login(string username, string password, string role)
            {
                using (SqlConnection connection = database.GetConnection())
                {
                    connection.Open();

                    string query = @"
                    SELECT COUNT(*)
                    FROM Users
                    WHERE Username = @Username
                    AND [Password] = @Password
                    AND Role = @Role";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);
                        command.Parameters.AddWithValue("@Role", role);

                        int count = (int)command.ExecuteScalar();

                        return count > 0;
                    }
                }
            }
        }
    }
}