
using System;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LandingPage : Form
    {
        private string currentRole;
        private Form currentChildForm;

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
            if (currentChildForm != null)
            {
                pnlMainContent.Controls.Remove(currentChildForm);

                currentChildForm.Close();
                currentChildForm.Dispose();

                currentChildForm = null;
            }

            currentChildForm = childForm;

            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;

            pnlMainContent.Controls.Clear();
            pnlMainContent.Controls.Add(childForm);

            childForm.Show();
        }

      
        private void lblProductManagement_Click(object sender, EventArgs e)
        {
            if (currentRole == "Admin" || currentRole == "Inventory")
            {
                OpenForm(new ProductManagement());
            }
        }

        private void lblSalesTransaction_Click(object sender, EventArgs e)
        {
            if (currentRole == "Admin" || currentRole == "Cashier")
            {
                SalesTransaction salesForm = new SalesTransaction();

             
                salesForm.PaymentRequested += (saleId) =>
                {
                    OpenForm(new Payment_Form(saleId));
                };

                OpenForm(salesForm);
            }
        }

 
        private void lblPayment_Click(object sender, EventArgs e)
        {
            if (currentRole == "Admin" || currentRole == "Cashier")
            {
                OpenForm(new Payment_Form());
            }
        }

      
        private void lblInventoryUpdate_Click(object sender, EventArgs e)
        {
            if (currentRole == "Admin" || currentRole == "Inventory")
            {
                OpenForm(new InventoryUpdate());
            }
        }

        private void lblReports_Click(object sender, EventArgs e)
        {
            if (currentRole == "Admin")
            {
                OpenForm(new Reports_Form());
            }
        }

      
        private void lblLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to logout?",
                "Confirm Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                LogoutRequested?.Invoke(this, EventArgs.Empty);
            }
        }

       
        private void ApplyRolePermissions()
        {
            if (currentRole == "Admin")
            {
               
                lblProductManagement.Visible = true;
                lblSalesTransaction.Visible = true;
                lblPayment.Visible = true;
                lblInventoryUpdate.Visible = true;
                lblReports.Visible = true;
            }
            else if (currentRole == "Cashier")
            {
              
                lblProductManagement.Visible = false;
                lblSalesTransaction.Visible = true;
                lblPayment.Visible = true;
                lblInventoryUpdate.Visible = false;
                lblReports.Visible = false;
            }
            else if (currentRole == "Inventory")
            {
           
                lblProductManagement.Visible = true;
                lblSalesTransaction.Visible = false;
                lblPayment.Visible = false;
                lblInventoryUpdate.Visible = true;
                lblReports.Visible = false;
            }
            else
            {
                
                lblProductManagement.Visible = false;
                lblSalesTransaction.Visible = false;
                lblPayment.Visible = false;
                lblInventoryUpdate.Visible = false;
                lblReports.Visible = false;
            }
        }

        private void LandingPage_Load(object sender, EventArgs e)
        {
        }

     
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (currentChildForm != null)
            {
                currentChildForm.Close();
                currentChildForm.Dispose();
                currentChildForm = null;
            }

            base.OnFormClosed(e);
        }
    }
}
