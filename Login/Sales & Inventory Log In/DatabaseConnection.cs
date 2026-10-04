
using System.Data.SqlClient;

namespace Sales_Inventory_Log_In
{
    public class DatabaseConnection
    {
        private readonly string connectionString =
            @"Server=.\SQLEXPRESS;Database=SalesInventoryDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}