using System;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LoginForm : Form
    {
        private readonly AuthService authService = new AuthService();
        private bool openingLandingPage;

        public LoginForm()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true;
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
                          rbInventory.Checked ? "Inventory" : string.Empty;

            if (username.Length == 0 || password.Length == 0 || role.Length == 0)
            {
                MessageBox.Show("Enter your username, password, and select your role.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (!authService.Login(username, password, role))
                {
                    MessageBox.Show("Invalid username, password, or role.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                openingLandingPage = true;
                LandingPage landing = new LandingPage(role);
                landing.FormClosed += delegate
                {
                    txtPassword.Clear();
                    txtUsername.Clear();
                    openingLandingPage = false;
                    if (!IsDisposed)
                        Show();
                };
                landing.LogoutRequested += delegate
                {
                    landing.Close();
                };

                Hide();
                landing.Show();
            }
            catch (Exception ex)
            {
                openingLandingPage = false;
                MessageBox.Show("Unable to log in. Check your database connection and Users table.\n\n" + ex.Message, "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
