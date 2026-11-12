using System;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class LandingPage : Form
    {
        private readonly string currentRole;
        private Form currentChildForm;
        private bool isOpeningForm;

        public event EventHandler LogoutRequested;

        public LandingPage(string role)
        {
            InitializeComponent();
            currentRole = role ?? string.Empty;
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
            if (childForm == null || IsDisposed || pnlMainContent.IsDisposed)
                return;

            try
            {
                if (currentChildForm != null)
                {
                    pnlMainContent.Controls.Remove(currentChildForm);
                    currentChildForm.Close();
                    currentChildForm.Dispose();
                    currentChildForm = null;
                }

                pnlMainContent.Controls.Clear();
                currentChildForm = childForm;
                childForm.TopLevel = false;
                childForm.FormBorderStyle = FormBorderStyle.None;
                childForm.Dock = DockStyle.Fill;
                pnlMainContent.Controls.Add(childForm);
                childForm.Show();
                childForm.BringToFront();
            }
            catch (Exception ex)
            {
                if (childForm != currentChildForm)
                    childForm.Dispose();
                MessageBox.Show("Unable to open this form.\n\n" + ex.Message, "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowNavigationError(Exception ex)
        {
            MessageBox.Show("The selected page could not be opened. Check SQL Server and the required database tables.\n\n" + ex.Message, "Form Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void lblProductManagement_Click(object sender, EventArgs e)
        {
            if (currentRole != "Admin" && currentRole != "Inventory") return;
            try { OpenForm(new ProductManagement()); }
            catch (Exception ex) { ShowNavigationError(ex); }
        }

        private void lblSalesTransaction_Click(object sender, EventArgs e)
        {
            if (currentRole != "Admin" && currentRole != "Cashier") return;
            if (isOpeningForm) return;

            try
            {
                isOpeningForm = true;
                SalesTransaction salesForm = new SalesTransaction();
                salesForm.PaymentRequested += delegate (int saleId)
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke((Action)delegate
                    {
                        if (!IsDisposed)
                        {
                            try { OpenForm(new Payment_Form(saleId)); }
                            catch (Exception ex) { ShowNavigationError(ex); }
                        }
                    });
                };
                OpenForm(salesForm);
            }
            catch (Exception ex)
            {
                ShowNavigationError(ex);
            }
            finally
            {
                isOpeningForm = false;
            }
        }

        private void lblPayment_Click(object sender, EventArgs e)
        {
            if (currentRole != "Admin" && currentRole != "Cashier") return;
            try { OpenForm(new Payment_Form()); }
            catch (Exception ex) { ShowNavigationError(ex); }
        }

        private void lblInventoryUpdate_Click(object sender, EventArgs e)
        {
            if (currentRole != "Admin" && currentRole != "Inventory") return;
            try { OpenForm(new InventoryUpdate()); }
            catch (Exception ex) { ShowNavigationError(ex); }
        }

        private void lblReports_Click(object sender, EventArgs e)
        {
            if (currentRole != "Admin") return;
            try { OpenForm(new Reports_Form()); }
            catch (Exception ex) { ShowNavigationError(ex); }
        }

        private void lblLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void ApplyRolePermissions()
        {
            bool admin = currentRole == "Admin";
            bool cashier = currentRole == "Cashier";
            bool inventory = currentRole == "Inventory";

            lblProductManagement.Visible = admin || inventory;
            lblSalesTransaction.Visible = admin || cashier;
            lblPayment.Visible = admin || cashier;
            lblInventoryUpdate.Visible = admin || inventory;
            lblReports.Visible = admin;
        }

        private void LandingPage_Load(object sender, EventArgs e)
        {
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (currentChildForm != null)
            {
                pnlMainContent.Controls.Remove(currentChildForm);
                currentChildForm.Close();
                currentChildForm.Dispose();
                currentChildForm = null;
            }
            base.OnFormClosed(e);
        }
    }
}
