```csharp
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class SalesTransaction : Form
    {
        private readonly DatabaseConnection database = new DatabaseConnection();
        private readonly DataTable cart = new DataTable();
        private bool paymentRequested;
        private readonly bool canEdit;

        public event Action<int> PaymentRequested;

        public SalesTransaction() : this(true)
        {
        }

        public SalesTransaction(bool canEdit)
        {
            InitializeComponent();
            this.canEdit = canEdit;

            txtInvoiceNumber.ReadOnly = true;
            txtInvoiceNumber.Text = CreateInvoiceNumber();
            dtpTransactionDate.Value = DateTime.Now;

            nudQuantity.Minimum = 1;
            nudQuantity.Maximum = 9999;
            nudQuantity.Value = 1;

            cart.Columns.Add("ProductID", typeof(int));
            cart.Columns.Add("ProductName", typeof(string));
            cart.Columns.Add("Price", typeof(decimal));
            cart.Columns.Add("Quantity", typeof(int));
            cart.Columns.Add("Subtotal", typeof(decimal));
            cart.Columns.Add("AvailableStock", typeof(int));

            dgvCartItems.DataSource = cart;
            dgvCartItems.AllowUserToAddRows = false;
            dgvCartItems.ReadOnly = true;
            dgvCartItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCartItems.MultiSelect = false;
            dgvCartItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCartItems.Columns["ProductID"].Visible = false;
            dgvCartItems.Columns["AvailableStock"].Visible = false;

            btnAddToCart.Click += btnAddToCart_Click;
            btnRemoveCartItem.Click += btnRemoveCartItem_Click;
            btnClearCart.Click += btnClearCart_Click;
            btnProceedToPayment.Click += btnProceedToPayment_Click;

            SetSalesPermissions();
            LoadProducts();
            UpdateTotal();
        }

        private void SetSalesPermissions()
        {
            btnAddToCart.Enabled = canEdit;
            btnRemoveCartItem.Enabled = canEdit;
            btnClearCart.Enabled = canEdit;
            btnProceedToPayment.Enabled = canEdit;

            txtCustomerName.ReadOnly = !canEdit;
            cmbProduct.Enabled = canEdit;
            nudQuantity.Enabled = canEdit;
            dtpTransactionDate.Enabled = canEdit;
        }

        private string CreateInvoiceNumber()
        {
            return "INV-" + Guid.NewGuid().ToString("N")
                .Substring(0, 8).ToUpperInvariant();
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT ProductID, ProductName, Price, StockQuantity
                    FROM Products
                    WHERE IsActive = 1 AND StockQuantity > 0
                    ORDER BY ProductName;", con))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbProduct.DataSource = null;
                    cmbProduct.DisplayMember = "ProductName";
                    cmbProduct.ValueMember = "ProductID";
                    cmbProduct.DataSource = table;
                    cmbProduct.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                cmbProduct.DataSource = null;

                MessageBox.Show(
                    "Products could not be loaded. Verify that SQL Server is running and the Products table exists.\n\n" + ex.Message,
                    "Product Loading Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void btnAddToCart_Click(object sender, EventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("You are not authorized to modify the sales cart.");
                return;
            }

            if (cmbProduct.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a product.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                MessageBox.Show("Enter the customer name first.");
                txtCustomerName.Focus();
                return;
            }

            DataRowView selected = cmbProduct.SelectedItem as DataRowView;
            if (selected == null)
                return;

            int productId = Convert.ToInt32(selected["ProductID"]);
            string productName = Convert.ToString(selected["ProductName"]);
            decimal price = Convert.ToDecimal(selected["Price"]);
            int stock = Convert.ToInt32(selected["StockQuantity"]);
            int quantity = Convert.ToInt32(nudQuantity.Value);

            DataRow existing = null;

            foreach (DataRow row in cart.Rows)
            {
                if (Convert.ToInt32(row["ProductID"]) == productId)
                {
                    existing = row;
                    break;
                }
            }

            int currentQuantity = existing == null
                ? 0
                : Convert.ToInt32(existing["Quantity"]);

            if (currentQuantity + quantity > stock)
            {
                MessageBox.Show("The requested quantity exceeds the available stock.");
                return;
            }

            if (existing == null)
            {
                cart.Rows.Add(
                    productId,
                    productName,
                    price,
                    quantity,
                    price * quantity,
                    stock);
            }
            else
            {
                int updatedQuantity = currentQuantity + quantity;
                existing["Quantity"] = updatedQuantity;
                existing["Subtotal"] = price * updatedQuantity;
            }

            UpdateTotal();
            nudQuantity.Value = 1;
        }

        private decimal GetTotal()
        {
            decimal total = 0m;

            foreach (DataRow row in cart.Rows)
                total += Convert.ToDecimal(row["Subtotal"]);

            return total;
        }

        private void UpdateTotal()
        {
            lblTotalAmount.Text = "₱" + GetTotal().ToString("N2");
        }

        private void btnRemoveCartItem_Click(object sender, EventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("You are not authorized to modify the sales cart.");
                return;
            }

            if (dgvCartItems.CurrentRow == null ||
                dgvCartItems.CurrentRow.IsNewRow)
            {
                MessageBox.Show("Select an item to remove.");
                return;
            }

            DataRowView selected =
                dgvCartItems.CurrentRow.DataBoundItem as DataRowView;

            if (selected != null)
            {
                cart.Rows.Remove(selected.Row);
                UpdateTotal();
            }
        }

        private void btnClearCart_Click(object sender, EventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("You are not authorized to modify the sales cart.");
                return;
            }

            if (cart.Rows.Count == 0)
                return;

            if (MessageBox.Show(
                "Clear all items from this cart?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cart.Clear();
                UpdateTotal();
            }
        }

        private void btnProceedToPayment_Click(object sender, EventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("You are not authorized to process sales.");
                return;
            }

            if (paymentRequested)
                return;

            if (cart.Rows.Count == 0)
            {
                MessageBox.Show("Your cart is empty.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                MessageBox.Show("Enter the customer name.");
                txtCustomerName.Focus();
                return;
            }

            int saleId = 0;

            using (SqlConnection con = database.GetConnection())
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand command = new SqlCommand(@"
                            INSERT INTO Sales
                            (InvoiceNumber, CustomerName, SaleDate, TotalAmount, PaymentStatus, StockDeducted)
                            VALUES
                            (@Invoice, @Customer, @SaleDate, @Total, 'Pending', 0);

                            SELECT CAST(SCOPE_IDENTITY() AS INT);",
                            con, transaction))
                        {
                            command.Parameters.Add("@Invoice", SqlDbType.VarChar, 30)
                                .Value = txtInvoiceNumber.Text;

                            command.Parameters.Add("@Customer", SqlDbType.NVarChar, 150)
                                .Value = txtCustomerName.Text.Trim();

                            command.Parameters.Add("@SaleDate", SqlDbType.DateTime)
                                .Value = dtpTransactionDate.Value;

                            command.Parameters.Add("@Total", SqlDbType.Decimal)
                                .Value = GetTotal();

                            command.Parameters["@Total"].Precision = 18;
                            command.Parameters["@Total"].Scale = 2;

                            saleId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        foreach (DataRow row in cart.Rows)
                        {
                            using (SqlCommand command = new SqlCommand(@"
                                INSERT INTO SaleItems
                                (SaleID, ProductID, Quantity, UnitPrice, Subtotal)
                                VALUES
                                (@SaleID, @ProductID, @Quantity, @Price, @Subtotal);",
                                con, transaction))
                            {
                                command.Parameters.AddWithValue("@SaleID", saleId);
                                command.Parameters.AddWithValue(
                                    "@ProductID", Convert.ToInt32(row["ProductID"]));
                                command.Parameters.AddWithValue(
                                    "@Quantity", Convert.ToInt32(row["Quantity"]));
                                command.Parameters.AddWithValue(
                                    "@Price", Convert.ToDecimal(row["Price"]));
                                command.Parameters.AddWithValue(
                                    "@Subtotal", Convert.ToDecimal(row["Subtotal"]));

                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }

                        MessageBox.Show(
                            "Unable to save this sale. No sale items were committed.\n\n" + ex.Message,
                            "Sales Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return;
                    }
                }
            }

            paymentRequested = true;

            MessageBox.Show(
                "Sale saved successfully. Continue to payment.",
                "Sale Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            PaymentRequested?.Invoke(saleId);
        }

        private void SalesTransaction_Load(object sender, EventArgs e)
        {
        }
    }
}