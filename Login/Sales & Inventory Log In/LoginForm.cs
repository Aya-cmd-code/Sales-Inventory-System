using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LoginForm : Form
    {
        
    string selectedRole = "";

        public LoginForm()
        {
            InitializeComponent();
        }


        private void btnLogin_Click(object sender, EventArgs e)
        {
            {
                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text.Trim();

                if (selectedRole == "")
                {
                    MessageBox.Show("Please select a role.");
                    return;
                }

                switch (selectedRole)
                {
                    case "Admin":
                        if (username == "admin" && password == "admin123")
                        {
                            AdminForm admin = new AdminForm();
                            admin.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Invalid Admin username or password.");
                        }
                        break;

                    case "Cashier":
                        if (username == "cashier" && password == "cashier123")
                        {
                            CashierForm cashier = new CashierForm();
                            cashier.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Invalid Cashier username or password.");
                        }
                        break;

                    case "Inventory Staff":
                        if (username == "inventory" && password == "inventory123")
                        {
                            InventoryForm inventory = new InventoryForm();
                            inventory.Show();
                            this.Hide();
                        }
                        else
                        {
                            MessageBox.Show("Invalid Inventory Staff username or password.");
                        }
                        break;
                }
            }
        }

        private void btnAdmin_Click(object sender, EventArgs e)
        {
            selectedRole = "Admin";
        }

        private void btnInventory_Click(object sender, EventArgs e)
        {
            selectedRole = "Inventory Staff";
        }

        private void btnCashier_Click(object sender, EventArgs e)
        {
            selectedRole = "Cashier";
        }
    }
}
