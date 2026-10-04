using System;

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
}