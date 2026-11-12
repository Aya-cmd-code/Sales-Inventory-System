using System;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LoginForm : Form
    {
        private readonly AuthService authService = new AuthService();

        public LoginForm()
        {
            InitializeComponent();

            // Hide password characters
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.PasswordChar = '*';

            btnLogin.Click -= btnLogin_Click;
            btnLogin.Click += btnLogin_Click;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            string role = rbAdmin.Checked ? "Admin" :
                          rbCashier.Checked ? "Cashier" :
                          rbInventory.Checked ? "Inventory" : "";

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(role))
            {
                MessageBox.Show(
                    "Enter your username, password, and select your role.",
                    "Missing Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            try
            {
                if (!authService.Login(username, password, role))
                {
                    MessageBox.Show(
                        "Invalid username, password, or role.",
                        "Login Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

               LandingPage landing = new LandingPage(role);

                landing.FormClosed += (s, args) =>
                {
                    txtPassword.Clear();
                    txtUsername.Clear();
                    Show();
                };

                Hide();
                landing.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to log in: " + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}