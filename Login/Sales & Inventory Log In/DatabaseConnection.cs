
using System.Data.SqlClient;

namespace Sales___Inventory_Log_In
{
    public class DatabaseConnection
    {
        private readonly string connectionString =
            @"Data Source=.\SQLEXPRESS;
              Initial Catalog=SalesInventoryDB;
              Integrated Security=True;
              TrustServerCertificate=True;";

        public SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
