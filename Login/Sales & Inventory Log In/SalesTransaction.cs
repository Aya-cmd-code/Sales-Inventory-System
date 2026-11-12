
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class SalesTransaction : Form
    {
        private DatabaseConnection database = new DatabaseConnection();
        private DataTable cart = new DataTable();

        public event Action<int> PaymentRequested;
        public SalesTransaction()
        {
            InitializeComponent();

            txtInvoiceNumber.ReadOnly = true;
            txtInvoiceNumber.Text = "INV-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

            nudQuantity.Minimum = 1;
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
            dgvCartItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCartItems.Columns["ProductID"].Visible = false;
            dgvCartItems.Columns["AvailableStock"].Visible = false;

            btnAddToCart.Click += btnAddToCart_Click;
            btnRemoveCartItem.Click += btnRemoveCartItem_Click;
            btnClearCart.Click += btnClearCart_Click;
            btnProceedToPayment.Click += btnProceedToPayment_Click;

            LoadProducts();
            UpdateTotal();
        }

        private void LoadProducts()
        {
            using (SqlConnection con = database.GetConnection())
            {
                string query = @"
                    SELECT ProductID, ProductName, Price, StockQuantity
                    FROM Products
                    WHERE IsActive = 1 AND Status <> 'Out of Stock'
                    AND StockQuantity > 0
                    ORDER BY ProductName";

                SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                DataTable table = new DataTable();
                adapter.Fill(table);

                cmbProduct.DataSource = table;
                cmbProduct.DisplayMember = "ProductName";
                cmbProduct.ValueMember = "ProductID";
                cmbProduct.SelectedIndex = -1;
            }
        }

        private void btnAddToCart_Click(object sender, EventArgs e)
        {
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
            if (selected == null) return;

            int id = Convert.ToInt32(selected["ProductID"]);
            string name = selected["ProductName"].ToString();
            decimal price = Convert.ToDecimal(selected["Price"]);
            int stock = Convert.ToInt32(selected["StockQuantity"]);
            int quantity = Convert.ToInt32(nudQuantity.Value);

            DataRow existing = null;

            foreach (DataRow row in cart.Rows)
            {
                if (Convert.ToInt32(row["ProductID"]) == id)
                {
                    existing = row;
                    break;
                }
            }

            int oldQuantity = existing == null
                ? 0 : Convert.ToInt32(existing["Quantity"]);

            if (oldQuantity + quantity > stock)
            {
                MessageBox.Show("The quantity exceeds the available stock.");
                return;
            }

            if (existing == null)
            {
                cart.Rows.Add(id, name, price, quantity,
                    price * quantity, stock);
            }
            else
            {
                int newQuantity = oldQuantity + quantity;
                existing["Quantity"] = newQuantity;
                existing["Subtotal"] = price * newQuantity;
            }

            UpdateTotal();
            nudQuantity.Value = 1;
        }

        private decimal GetTotal()
        {
            decimal total = 0;

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
            if (dgvCartItems.CurrentRow == null ||
                dgvCartItems.CurrentRow.Index < 0)
            {
                MessageBox.Show("Select an item to remove.");
                return;
            }

            int index = dgvCartItems.CurrentRow.Index;

            if (index < cart.Rows.Count)
            {
                cart.Rows.RemoveAt(index);
                UpdateTotal();
            }
        }

        private void btnClearCart_Click(object sender, EventArgs e)
        {
            cart.Clear();
            UpdateTotal();
        }


        private void btnProceedToPayment_Click(object sender, EventArgs e)
        {
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

                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    string saleQuery = @"
                INSERT INTO Sales
                (InvoiceNumber, CustomerName, SaleDate,
                 TotalAmount, PaymentStatus)
                VALUES
                (@Invoice, @Customer, @Date, @Total, 'Pending');

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand cmd =
                        new SqlCommand(saleQuery, con, trans))
                    {
                        cmd.Parameters.AddWithValue(
                            "@Invoice", txtInvoiceNumber.Text);

                        cmd.Parameters.AddWithValue(
                            "@Customer", txtCustomerName.Text.Trim());

                        cmd.Parameters.AddWithValue(
                            "@Date", dtpTransactionDate.Value);

                        cmd.Parameters.AddWithValue(
                            "@Total", GetTotal());

                        saleId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    foreach (DataRow row in cart.Rows)
                    {
                        string itemQuery = @"
                    INSERT INTO SaleItems
                    (SaleID, ProductID, Quantity, UnitPrice, Subtotal)
                    VALUES
                    (@SaleID, @ProductID, @Quantity, @Price, @Subtotal)";

                        using (SqlCommand cmd =
                            new SqlCommand(itemQuery, con, trans))
                        {
                            cmd.Parameters.AddWithValue("@SaleID", saleId);
                            cmd.Parameters.AddWithValue("@ProductID", row["ProductID"]);
                            cmd.Parameters.AddWithValue("@Quantity", row["Quantity"]);
                            cmd.Parameters.AddWithValue("@Price", row["Price"]);
                            cmd.Parameters.AddWithValue("@Subtotal", row["Subtotal"]);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    trans.Commit();

                    MessageBox.Show(
                        "Sale saved successfully. Proceed to payment."
                    );

                    cart.Clear();
                    UpdateTotal();
                    txtCustomerName.Clear();

                    txtInvoiceNumber.Text =
                        "INV-" + Guid.NewGuid().ToString("N")
                        .Substring(0, 8).ToUpper();

                    LoadProducts();

                    // Open Payment only after the Sales form has reset.
                    PaymentRequested?.Invoke(saleId);
                }
                catch (Exception ex)
                {
                    trans.Rollback();

                    MessageBox.Show(
                        "Unable to save sale: " + ex.Message
                    );
                }
            }
        }
       

        private void SalesTransaction_Load(object sender, EventArgs e)
        {

        }
    }
}
