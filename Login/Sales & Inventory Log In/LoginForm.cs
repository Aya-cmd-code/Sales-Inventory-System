using Sales_Inventory_Log_In;
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
            private AuthService authService;
            private bool dashboardOpened = false;

            public LoginForm()
            {
                InitializeComponent();

                authService = new AuthService();

                txtPassword.PasswordChar = '●';

                rbAdmin.Checked = true;

                btnLogin.Click -= btnLogin_Click;
                btnLogin.Click += btnLogin_Click;
            }

            private void btnLogin_Click(object sender, EventArgs e)
            {
                if (dashboardOpened)
                    return;

                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text;

                string selectedRole = "";

                if (rbAdmin.Checked)
                {
                    selectedRole = "Admin";
                }
                else if (rbCashier.Checked)
                {
                    selectedRole = "Cashier";
                }
                else if (rbInventory.Checked)
                {
                    selectedRole = "Inventory";
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show(
                        "Please enter your username.",
                        "Login",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show(
                        "Please enter your password.",
                        "Login",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    txtPassword.Focus();
                    return;
                }

                bool loginSuccessful = authService.Login(
                    username,
                    password,
                    selectedRole
                );

                if (loginSuccessful)
                {
                    dashboardOpened = true;

                    MessageBox.Show(
                        "Login successful!",
                        "Welcome",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    OpenDashboard(selectedRole);
                }
                else
                {
                    MessageBox.Show(
                        "Invalid username, password, or role.",
                        "Login Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );

                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }

            private void OpenDashboard(string role)
            {
                Form dashboard = null;

                if (role == "Admin")
                {
                    dashboard = new AdminForm();
                }
                else if (role == "Cashier")
                {
                    dashboard = new CashierForm();
                }
                else if (role == "Inventory")
                {
                    dashboard = new InventoryForm();
                }

                if (dashboard != null)
                {
                    this.Hide();

                    dashboard.FormClosed += Dashboard_FormClosed;

                    dashboard.Show();
                }
                else
                {
                    dashboardOpened = false;

                    MessageBox.Show(
                        "Unable to open the selected dashboard.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }

            private void Dashboard_FormClosed(object sender, FormClosedEventArgs e)
            {
                this.Close();
            }

            private void LoginForm_Load(object sender, EventArgs e)
            {
            }
        }
    }
