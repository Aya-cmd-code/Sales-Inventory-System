using Sales_Inventory_Log_In;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class CashierForm : Form
    {
        private DatabaseConnection database;
        private int selectedSaleID = 0;
        public CashierForm()
        {
            InitializeComponent();

            database = new DatabaseConnection();

            txtUnitPrice.ReadOnly = true;
            txtTotalAmount.ReadOnly = true;

            btnAddSale.Click -= btnAddSale_Click;
            btnAddSale.Click += btnAddSale_Click;

            btnUpdateSale.Click -= btnUpdateSale_Click;
            btnUpdateSale.Click += btnUpdateSale_Click;

            btnDeleteSale.Click -= btnDeleteSale_Click;
            btnDeleteSale.Click += btnDeleteSale_Click;

            btnRefreshSale.Click -= btnRefreshSale_Click;
            btnRefreshSale.Click += btnRefreshSale_Click;

            cmbProduct.SelectedIndexChanged -= cmbProduct_SelectedIndexChanged;
            cmbProduct.SelectedIndexChanged += cmbProduct_SelectedIndexChanged;

            txtQuantity.TextChanged -= txtQuantity_TextChanged;
            txtQuantity.TextChanged += txtQuantity_TextChanged;

            dgvSales.CellClick -= dgvSales_CellClick;
            dgvSales.CellClick += dgvSales_CellClick;

            LoadCashierDashboard();
        }

        private void LoadCashierDashboard()
        {
            try
            {
                LoadCustomers();
                LoadProducts();
                LoadTransactions();
                LoadStatistics();
                ClearSaleFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error loading cashier dashboard:\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoadCustomers()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query =
                    "SELECT CustomerID, CustomerName FROM Customers ORDER BY CustomerName";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbCustomer.DisplayMember = "CustomerName";
                    cmbCustomer.ValueMember = "CustomerID";
                    cmbCustomer.DataSource = table;
                    cmbCustomer.SelectedIndex = -1;
                }
            }
        }

        private void LoadProducts()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        ProductID,
                        ProductName,
                        Price,
                        StockQty
                    FROM Products
                    WHERE StockQty > 0
                    ORDER BY ProductName";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbProduct.DisplayMember = "ProductName";
                    cmbProduct.ValueMember = "ProductID";
                    cmbProduct.DataSource = table;
                    cmbProduct.SelectedIndex = -1;
                }
            }
        }

        private void LoadTransactions()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        s.SaleID,
                        s.InvoiceNo,
                        ISNULL(c.CustomerName, 'Walk-in Customer') AS Customer,
                        p.ProductName,
                        si.Quantity,
                        si.UnitPrice,
                        si.Total,
                        s.SaleDate
                    FROM Sales s
                    INNER JOIN SaleItems si
                        ON s.SaleID = si.SaleID
                    INNER JOIN Products p
                        ON si.ProductID = p.ProductID
                    LEFT JOIN Customers c
                        ON s.CustomerID = c.CustomerID
                    ORDER BY s.SaleDate DESC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    dgvSales.DataSource = table;
                    dgvRecentTransactions.DataSource = table.Copy();
                }
            }

            dgvSales.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            dgvRecentTransactions.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadStatistics()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        ISNULL(SUM(TotalAmount), 0),
                        COUNT(*),
                        (
                            SELECT COUNT(DISTINCT CustomerID)
                            FROM Sales
                            WHERE CustomerID IS NOT NULL
                        ),
                        ISNULL(AVG(TotalAmount), 0)
                    FROM Sales
                    WHERE CAST(SaleDate AS DATE) = CAST(GETDATE() AS DATE)";

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string todaySales =
                                "₱" +
                                Convert.ToDecimal(reader[0]).ToString("N2");

                            string transactions =
                                Convert.ToInt32(reader[1]).ToString();

                            string customers =
                                Convert.ToInt32(reader[2]).ToString();

                            string average =
                                "₱" +
                                Convert.ToDecimal(reader[3]).ToString("N2");

                            SetLabelIfExists(
                                "lblTodaySales",
                                todaySales);

                            SetLabelIfExists(
                                "lblTransactions",
                                transactions);

                            SetLabelIfExists(
                                "lblCustomerServed",
                                customers);

                            SetLabelIfExists(
                                "lblAverageOrderValue",
                                average);
                        }
                    }
                }
            }
        }

        private void SetLabelIfExists(string name, string value)
        {
            Control[] controls = Controls.Find(name, true);

            if (controls.Length > 0)
            {
                controls[0].Text = value;
            }
        }

        private void cmbProduct_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            if (cmbProduct.SelectedIndex == -1)
                return;

            DataRowView row = cmbProduct.SelectedItem as DataRowView;

            if (row == null)
                return;

            txtUnitPrice.Text =
                Convert.ToDecimal(row["Price"]).ToString("N2");

            CalculateTotal();
        }

        private void txtQuantity_TextChanged(
            object sender,
            EventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            decimal price;
            int quantity;

            if (decimal.TryParse(txtUnitPrice.Text, out price) &&
                int.TryParse(txtQuantity.Text, out quantity) &&
                quantity > 0)
            {
                txtTotalAmount.Text =
                    (price * quantity).ToString("N2");
            }
            else
            {
                txtTotalAmount.Clear();
            }
        }

        private bool ValidateSale()
        {
            if (string.IsNullOrWhiteSpace(txtInvoiceNo.Text))
            {
                MessageBox.Show("Enter invoice number.");
                txtInvoiceNo.Focus();
                return false;
            }

            if (cmbCustomer.SelectedIndex == -1)
            {
                MessageBox.Show("Select a customer.");
                cmbCustomer.Focus();
                return false;
            }

            if (cmbProduct.SelectedIndex == -1)
            {
                MessageBox.Show("Select a product.");
                cmbProduct.Focus();
                return false;
            }

            int quantity;

            if (!int.TryParse(txtQuantity.Text, out quantity) ||
                quantity <= 0)
            {
                MessageBox.Show("Enter a valid quantity.");
                txtQuantity.Focus();
                return false;
            }

            return true;
        }

        private void btnAddSale_Click(object sender, EventArgs e)
        {
            if (!ValidateSale())
                return;

            int customerID =
                Convert.ToInt32(cmbCustomer.SelectedValue);

            int productID =
                Convert.ToInt32(cmbProduct.SelectedValue);

            int quantity =
                int.Parse(txtQuantity.Text);

            decimal unitPrice =
                decimal.Parse(txtUnitPrice.Text);

            decimal total =
                decimal.Parse(txtTotalAmount.Text);

            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                SqlTransaction transaction =
                    connection.BeginTransaction();

                try
                {
                    string stockQuery = @"
                        SELECT StockQty
                        FROM Products
                        WHERE ProductID = @ProductID";

                    int stock;

                    using (SqlCommand stockCommand =
                        new SqlCommand(
                            stockQuery,
                            connection,
                            transaction))
                    {
                        stockCommand.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        stock =
                            Convert.ToInt32(
                                stockCommand.ExecuteScalar());
                    }

                    if (stock < quantity)
                    {
                        transaction.Rollback();

                        MessageBox.Show(
                            "Not enough stock available.",
                            "Insufficient Stock",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        return;
                    }

                    string saleQuery = @"
                        INSERT INTO Sales
                        (
                            InvoiceNo,
                            CustomerID,
                            TotalAmount,
                            SaleDate
                        )
                        OUTPUT INSERTED.SaleID
                        VALUES
                        (
                            @InvoiceNo,
                            @CustomerID,
                            @TotalAmount,
                            GETDATE()
                        )";

                    int saleID;

                    using (SqlCommand saleCommand =
                        new SqlCommand(
                            saleQuery,
                            connection,
                            transaction))
                    {
                        saleCommand.Parameters.AddWithValue(
                            "@InvoiceNo",
                            txtInvoiceNo.Text.Trim());

                        saleCommand.Parameters.AddWithValue(
                            "@CustomerID",
                            customerID);

                        saleCommand.Parameters.AddWithValue(
                            "@TotalAmount",
                            total);

                        saleID =
                            Convert.ToInt32(
                                saleCommand.ExecuteScalar());
                    }

                    string itemQuery = @"
                        INSERT INTO SaleItems
                        (
                            SaleID,
                            ProductID,
                            Quantity,
                            UnitPrice,
                            Total
                        )
                        VALUES
                        (
                            @SaleID,
                            @ProductID,
                            @Quantity,
                            @UnitPrice,
                            @Total
                        )";

                    using (SqlCommand itemCommand =
                        new SqlCommand(
                            itemQuery,
                            connection,
                            transaction))
                    {
                        itemCommand.Parameters.AddWithValue(
                            "@SaleID",
                            saleID);

                        itemCommand.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        itemCommand.Parameters.AddWithValue(
                            "@Quantity",
                            quantity);

                        itemCommand.Parameters.AddWithValue(
                            "@UnitPrice",
                            unitPrice);

                        itemCommand.Parameters.AddWithValue(
                            "@Total",
                            total);

                        itemCommand.ExecuteNonQuery();
                    }

                    string updateStockQuery = @"
                        UPDATE Products
                        SET
                            StockQty = StockQty - @Quantity,
                            LastUpdated = GETDATE()
                        WHERE ProductID = @ProductID";

                    using (SqlCommand stockUpdate =
                        new SqlCommand(
                            updateStockQuery,
                            connection,
                            transaction))
                    {
                        stockUpdate.Parameters.AddWithValue(
                            "@Quantity",
                            quantity);

                        stockUpdate.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        stockUpdate.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    MessageBox.Show("Sale added successfully.");

                    LoadCashierDashboard();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    MessageBox.Show(
                        ex.Message,
                        "Sale Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void btnUpdateSale_Click(object sender, EventArgs e)
        {
            if (selectedSaleID == 0)
            {
                MessageBox.Show("Select a sale first.");
                return;
            }

            if (!ValidateSale())
                return;

            int customerID =
                Convert.ToInt32(cmbCustomer.SelectedValue);

            int productID =
                Convert.ToInt32(cmbProduct.SelectedValue);

            int newQuantity =
                int.Parse(txtQuantity.Text);

            decimal unitPrice =
                decimal.Parse(txtUnitPrice.Text);

            decimal total =
                decimal.Parse(txtTotalAmount.Text);

            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                SqlTransaction transaction =
                    connection.BeginTransaction();

                try
                {
                    int oldProductID;
                    int oldQuantity;

                    string oldQuery = @"
                        SELECT ProductID, Quantity
                        FROM SaleItems
                        WHERE SaleID = @SaleID";

                    using (SqlCommand oldCommand =
                        new SqlCommand(
                            oldQuery,
                            connection,
                            transaction))
                    {
                        oldCommand.Parameters.AddWithValue(
                            "@SaleID",
                            selectedSaleID);

                        using (SqlDataReader reader =
                            oldCommand.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                reader.Close();
                                transaction.Rollback();

                                MessageBox.Show("Sale not found.");
                                return;
                            }

                            oldProductID =
                                Convert.ToInt32(reader["ProductID"]);

                            oldQuantity =
                                Convert.ToInt32(reader["Quantity"]);
                        }
                    }

                    string restoreQuery = @"
                        UPDATE Products
                        SET StockQty = StockQty + @Quantity,
                            LastUpdated = GETDATE()
                        WHERE ProductID = @ProductID";

                    using (SqlCommand restoreCommand =
                        new SqlCommand(
                            restoreQuery,
                            connection,
                            transaction))
                    {
                        restoreCommand.Parameters.AddWithValue(
                            "@Quantity",
                            oldQuantity);

                        restoreCommand.Parameters.AddWithValue(
                            "@ProductID",
                            oldProductID);

                        restoreCommand.ExecuteNonQuery();
                    }

                    int availableStock;

                    string stockQuery = @"
                        SELECT StockQty
                        FROM Products
                        WHERE ProductID = @ProductID";

                    using (SqlCommand stockCommand =
                        new SqlCommand(
                            stockQuery,
                            connection,
                            transaction))
                    {
                        stockCommand.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        availableStock =
                            Convert.ToInt32(
                                stockCommand.ExecuteScalar());
                    }

                    if (availableStock < newQuantity)
                    {
                        transaction.Rollback();

                        MessageBox.Show(
                            "Not enough stock available.",
                            "Insufficient Stock",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        return;
                    }

                    string updateSaleQuery = @"
                        UPDATE Sales
                        SET
                            InvoiceNo = @InvoiceNo,
                            CustomerID = @CustomerID,
                            TotalAmount = @TotalAmount
                        WHERE SaleID = @SaleID";

                    using (SqlCommand command =
                        new SqlCommand(
                            updateSaleQuery,
                            connection,
                            transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@InvoiceNo",
                            txtInvoiceNo.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@CustomerID",
                            customerID);

                        command.Parameters.AddWithValue(
                            "@TotalAmount",
                            total);

                        command.Parameters.AddWithValue(
                            "@SaleID",
                            selectedSaleID);

                        command.ExecuteNonQuery();
                    }

                    string updateItemQuery = @"
                        UPDATE SaleItems
                        SET
                            ProductID = @ProductID,
                            Quantity = @Quantity,
                            UnitPrice = @UnitPrice,
                            Total = @Total
                        WHERE SaleID = @SaleID";

                    using (SqlCommand command =
                        new SqlCommand(
                            updateItemQuery,
                            connection,
                            transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.Parameters.AddWithValue(
                            "@Quantity",
                            newQuantity);

                        command.Parameters.AddWithValue(
                            "@UnitPrice",
                            unitPrice);

                        command.Parameters.AddWithValue(
                            "@Total",
                            total);

                        command.Parameters.AddWithValue(
                            "@SaleID",
                            selectedSaleID);

                        command.ExecuteNonQuery();
                    }

                    string deductQuery = @"
                        UPDATE Products
                        SET StockQty = StockQty - @Quantity,
                            LastUpdated = GETDATE()
                        WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(
                            deductQuery,
                            connection,
                            transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@Quantity",
                            newQuantity);

                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    MessageBox.Show("Sale updated successfully.");

                    LoadCashierDashboard();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    MessageBox.Show(
                        ex.Message,
                        "Update Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void btnDeleteSale_Click(object sender, EventArgs e)
        {
            if (selectedSaleID == 0)
            {
                MessageBox.Show("Select a sale first.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Delete this sale?",
                "Delete Sale",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                SqlTransaction transaction =
                    connection.BeginTransaction();

                try
                {
                    int productID;
                    int quantity;

                    string findQuery = @"
                        SELECT ProductID, Quantity
                        FROM SaleItems
                        WHERE SaleID = @SaleID";

                    using (SqlCommand command =
                        new SqlCommand(
                            findQuery,
                            connection,
                            transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@SaleID",
                            selectedSaleID);

                        using (SqlDataReader reader =
                            command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                reader.Close();
                                transaction.Rollback();

                                MessageBox.Show("Sale not found.");
                                return;
                            }

                            productID =
                                Convert.ToInt32(reader["ProductID"]);

                            quantity =
                                Convert.ToInt32(reader["Quantity"]);
                        }
                    }

                    string restoreQuery = @"
                        UPDATE Products
                        SET
                            StockQty = StockQty + @Quantity,
                            LastUpdated = GETDATE()
                        WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(
                            restoreQuery,
                            connection,
                            transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@Quantity",
                            quantity);

                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.ExecuteNonQuery();
                    }

                    string deleteQuery =
                        "DELETE FROM Sales WHERE SaleID = @SaleID";

                    using (SqlCommand command =
                        new SqlCommand(
                            deleteQuery,
                            connection,
                            transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@SaleID",
                            selectedSaleID);

                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    MessageBox.Show("Sale deleted successfully.");

                    LoadCashierDashboard();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    MessageBox.Show(
                        ex.Message,
                        "Delete Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        private void btnRefreshSale_Click(object sender, EventArgs e)
        {
            LoadCashierDashboard();
        }

        private void dgvSales_CellClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = dgvSales.Rows[e.RowIndex];

            selectedSaleID =
                Convert.ToInt32(row.Cells["SaleID"].Value);

            txtInvoiceNo.Text =
                row.Cells["InvoiceNo"].Value?.ToString();

            string customer =
                row.Cells["Customer"].Value?.ToString();

            for (int i = 0; i < cmbCustomer.Items.Count; i++)
            {
                DataRowView item =
                    cmbCustomer.Items[i] as DataRowView;

                if (item != null &&
                    item["CustomerName"].ToString() == customer)
                {
                    cmbCustomer.SelectedIndex = i;
                    break;
                }
            }

            string product =
                row.Cells["ProductName"].Value?.ToString();

            for (int i = 0; i < cmbProduct.Items.Count; i++)
            {
                DataRowView item =
                    cmbProduct.Items[i] as DataRowView;

                if (item != null &&
                    item["ProductName"].ToString() == product)
                {
                    cmbProduct.SelectedIndex = i;
                    break;
                }
            }

            txtQuantity.Text =
                row.Cells["Quantity"].Value?.ToString();

            txtUnitPrice.Text =
                Convert.ToDecimal(
                    row.Cells["UnitPrice"].Value).ToString("N2");

            txtTotalAmount.Text =
                Convert.ToDecimal(
                    row.Cells["Total"].Value).ToString("N2");
        }

        private void ClearSaleFields()
        {
            selectedSaleID = 0;

            txtInvoiceNo.Clear();
            cmbCustomer.SelectedIndex = -1;
            cmbProduct.SelectedIndex = -1;
            txtQuantity.Clear();
            txtUnitPrice.Clear();
            txtTotalAmount.Clear();
        }
    }
}

