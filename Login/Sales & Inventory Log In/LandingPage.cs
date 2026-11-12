
using System;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LandingPage : Form
    {
        private string currentRole;

        public event EventHandler LogoutRequested;

        public LandingPage(string role)
        {
            InitializeComponent();

            currentRole = role;

            lblWelcome.Text = "Welcome, " + currentRole;

           
            lblProductManagement.Click += lblProductManagement_Click;
            lblSalesTransaction.Click += lblSalesTransaction_Click;
            lblPayment.Click += lblPayment_Click;
            lblInventoryUpdate.Click += lblInventoryUpdate_Click;
            lblReports.Click += lblReports_Click;
            lblLogout.Click += lblLogout_Click;

           

            ApplyRolePermissions();
        }

        private void OpenForm(Form childForm)
        {
            pnlMainContent.Controls.Clear();

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            pnlMainContent.Controls.Add(childForm);

            childForm.Show();
        }

      
        private void lblProductManagement_Click(object sender, EventArgs e)
        {
            OpenForm(new ProductManagement());
        }

        private void lblSalesTransaction_Click(object sender, EventArgs e)
        {
            OpenForm(new SalesTransaction());
        }

        private void lblPayment_Click(object sender, EventArgs e)
        {
            OpenForm(new Payment_Form());
        }

        private void lblInventoryUpdate_Click(object sender, EventArgs e)
        {
            OpenForm(new InventoryUpdate());
        }

        private void lblReports_Click(object sender, EventArgs e)
        {
            OpenForm(new Reports_Form());
        }

        private void lblLogout_Click(object sender, EventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyRolePermissions()
        {
            // ADMIN
            if (currentRole == "Admin")
            {
                
                lblProductManagement.Visible = true;
                lblSalesTransaction.Visible = true;
                lblPayment.Visible = true;
                lblInventoryUpdate.Visible = true;
                lblReports.Visible = true;
            }

            // CASHIER
            else if (currentRole == "Cashier")
            {
              
                lblProductManagement.Visible = false;
                lblSalesTransaction.Visible = true;
                lblPayment.Visible = true;
                lblInventoryUpdate.Visible = false;
                lblReports.Visible = false;
            }

            // INVENTORY
            else if (currentRole == "Inventory")
            {
              
                lblProductManagement.Visible = true;
                lblSalesTransaction.Visible = false;
                lblPayment.Visible = false;
                lblInventoryUpdate.Visible = true;
                lblReports.Visible = false;
            }
        }
    }
}
