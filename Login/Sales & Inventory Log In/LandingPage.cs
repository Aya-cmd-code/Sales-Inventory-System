```csharp
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

                MessageBox.Show("Unable to open this form.\n\n" + ex.Message,
                    "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowNavigationError(Exception ex)
        {
            MessageBox.Show("The selected page could not be opened.\n\n" + ex.Message,
                "Form Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private bool CanManage(string module)
        {
            string role = (currentRole ?? string.Empty).Trim().ToLowerInvariant();

            if (role == "admin")
                return module == "products" || module == "reports";

            if (role == "cashier")
                return module == "sales" || module == "payment";

            if (role == "inventory")
                return module == "inventory";

            return false;
        }

        private void lblProductManagement_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new ProductManagement(CanManage("products")));
            }
            catch (Exception ex)
            {
                ShowNavigationError(ex);
            }
        }

        private void lblSalesTransaction_Click(object sender, EventArgs e)
        {
            if (isOpeningForm)
                return;

            try
            {
                isOpeningForm = true;

                SalesTransaction salesForm = new SalesTransaction(CanManage("sales"));

                salesForm.PaymentRequested += delegate (int saleId)
                {
                    if (IsDisposed || !IsHandleCreated)
                        return;

                    BeginInvoke((Action)delegate
                    {
                        if (!IsDisposed)
                        {
                            try
                            {
                                OpenForm(new Payment_Form(saleId, CanManage("payment")));
                            }
                            catch (Exception ex)
                            {
                                ShowNavigationError(ex);
                            }
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
            try
            {
                OpenForm(new Payment_Form(CanManage("payment")));
            }
            catch (Exception ex)
            {
                ShowNavigationError(ex);
            }
        }

        private void lblInventoryUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new InventoryUpdate(CanManage("inventory")));
            }
            catch (Exception ex)
            {
                ShowNavigationError(ex);
            }
        }

        private void lblReports_Click(object sender, EventArgs e)
        {
            try
            {
                OpenForm(new Reports_Form(CanManage("reports")));
            }
            catch (Exception ex)
            {
                ShowNavigationError(ex);
            }
        }

        private void lblLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?",
                "Confirm Logout", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                LogoutRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ApplyRolePermissions()
        {
            lblProductManagement.Visible = true;
            lblSalesTransaction.Visible = true;
            lblPayment.Visible = true;
            lblInventoryUpdate.Visible = true;
            lblReports.Visible = true;
            lblLogout.Visible = true;
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

        private void pnlMainContent_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}