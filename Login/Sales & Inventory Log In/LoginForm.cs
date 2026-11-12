
using System;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LoginForm : Form
    {
        private bool openingLandingPage;

        public LoginForm()
        {
            InitializeComponent();

            txtPassword.UseSystemPasswordChar = true;
            txtPassword.PasswordChar = '*';

            btnLogin.Click -= btnLogin_Click;
            btnLogin.Click += btnLogin_Click;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (openingLandingPage)
                return;

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

            string correctPassword = "";

            switch (role)
            {
                case "Admin":
                    correctPassword = "admin123";
                    break;

                case "Inventory":
                    correctPassword = "inventory123";
                    break;

                case "Cashier":
                    correctPassword = "cashier123";
                    break;
            }

            if (password != correctPassword)
            {
                MessageBox.Show(
                    "Incorrect password for the selected role.",
                    "Login Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            try
            {
                openingLandingPage = true;

                LandingPage landing = new LandingPage(role);

                landing.LogoutRequested += delegate
                {
                    landing.Close();
                };

                landing.FormClosed += delegate
                {
                    txtUsername.Clear();
                    txtPassword.Clear();

                    rbAdmin.Checked = false;
                    rbCashier.Checked = false;
                    rbInventory.Checked = false;

                    openingLandingPage = false;

                    if (!IsDisposed)
                        Show();
                };

                Hide();
                landing.Show();
            }
            catch (Exception ex)
            {
                openingLandingPage = false;

                MessageBox.Show(
                    "Unable to open Landing Page.\n\n" + ex.Message,
                    "Login Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                Show();
            }
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
        }
    }
}
