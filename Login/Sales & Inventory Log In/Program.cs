using Sales___Inventory_Log_In;
using System;
using System.Windows.Forms;

namespace Sales_Inventory_Log_In
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}