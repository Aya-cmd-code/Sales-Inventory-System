
using System;
using System.Data.SqlClient;

namespace Sales___Inventory_Log_In
{
    public class AuthService
    {
        private DatabaseConnection database =
            new DatabaseConnection();

        public bool Login(
            string username,
            string password,
            string role)
        {
            using (SqlConnection connection =
                database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT COUNT(*)
                    FROM Users
                    WHERE Username = @Username
                    AND [Password] = @Password
                    AND Role = @Role";

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@Username", username);

                    command.Parameters.AddWithValue(
                        "@Password", password);

                    command.Parameters.AddWithValue(
                        "@Role", role);

                    int result =
                        Convert.ToInt32(
                            command.ExecuteScalar());

                    return result > 0;
                }
            }
        }
    }
}
