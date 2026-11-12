using System;
using System.Data.SqlClient;

namespace Sales___Inventory_Log_In
{
    public class DatabaseConnection
    {
        private readonly string connectionString =
            @"Data Source=.\SQLEXPRESS;Initial Catalog=SalesInventoryDB;Integrated Security=True;TrustServerCertificate=True;";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }

        public bool TestConnection(out string error)
        {
            try
            {
                using (SqlConnection connection = GetConnection())
                {
                    connection.Open();
                }
                error = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
