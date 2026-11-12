using System;
using System.Data.SqlClient;

namespace Sales___Inventory_Log_In
{
    public class AuthService
    {
        private readonly DatabaseConnection database = new DatabaseConnection();

        public bool Login(string username, string password, string role)
        {
            using (SqlConnection connection = database.GetConnection())
            using (SqlCommand command = new SqlCommand(@"
                SELECT COUNT(1)
                FROM Users
                WHERE Username = @Username
                  AND [Password] = @Password
                  AND Role = @Role
                  AND (IsActive = 1 OR IsActive IS NULL);", connection))
            {
                command.Parameters.AddWithValue("@Username", username.Trim());
                command.Parameters.AddWithValue("@Password", password);
                command.Parameters.AddWithValue("@Role", role);
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }
    }
}
