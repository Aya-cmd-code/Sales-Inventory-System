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
    public partial class AdminForm : Form
    {
        private DatabaseConnection database;
        public AdminForm()
        {
            database = new DatabaseConnection();

            InitializeComponent();

            cmbProductStatus.Items.Clear();
            cmbProductStatus.Items.Add("In Stock");
            cmbProductStatus.Items.Add("Low Stock");
            cmbProductStatus.Items.Add("Out of Stock");

            btnAddProduct.Click -= btnAddProduct_Click;
            btnAddProduct.Click += btnAddProduct_Click;

            btnUpdateProduct.Click -= btnUpdateProduct_Click;
            btnUpdateProduct.Click += btnUpdateProduct_Click;

            btnDeleteProduct.Click -= btnDeleteProduct_Click;
            btnDeleteProduct.Click += btnDeleteProduct_Click;

            btnRefreshProduct.Click -= btnRefreshProduct_Click;
            btnRefreshProduct.Click += btnRefreshProduct_Click;

            dgvProducts.CellClick -= dgvProducts_CellClick;
            dgvProducts.CellClick += dgvProducts_CellClick;

            LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                LoadStatistics();
                LoadProducts();
                LoadRecentOrders();
                LoadSalesOverview();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error loading dashboard:\n\n" + ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void LoadStatistics()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        (SELECT COUNT(*) FROM Products),
                        (SELECT ISNULL(SUM(TotalAmount), 0) FROM Sales),
                        (SELECT COUNT(*) FROM Sales),
                        (SELECT COUNT(*) FROM Customers)";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lblTotalProducts.Text =
                                Convert.ToInt32(reader[0]).ToString();

                            lblTotalSales.Text =
                                "₱" + Convert.ToDecimal(reader[1]).ToString("N2");

                            lblTotalOrders.Text =
                                Convert.ToInt32(reader[2]).ToString();

                            lblTotalCustomers.Text =
                                Convert.ToInt32(reader[3]).ToString();
                        }
                    }
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
                        SKU,
                        Price,
                        StockQty,
                        [Status],
                        ProductDate
                    FROM Products
                    ORDER BY ProductID DESC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvProducts.DataSource = table;
                }
            }

            dgvProducts.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadRecentOrders()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT TOP 10
                        s.InvoiceNo,
                        ISNULL(c.CustomerName, 'Walk-in Customer') AS Customer,
                        s.TotalAmount,
                        s.SaleDate
                    FROM Sales s
                    LEFT JOIN Customers c
                        ON s.CustomerID = c.CustomerID
                    ORDER BY s.SaleDate DESC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvRecentOrders.DataSource = table;
                }
            }

            dgvRecentOrders.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadSalesOverview()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        CAST(SaleDate AS DATE) AS SaleDate,
                        COUNT(*) AS Orders,
                        SUM(TotalAmount) AS TotalSales
                    FROM Sales
                    GROUP BY CAST(SaleDate AS DATE)
                    ORDER BY SaleDate DESC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvSalesOverview.DataSource = table;
                }
            }

            dgvSalesOverview.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private bool ValidateProduct()
        {
            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("Enter product name.");
                txtProductName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtSKU.Text))
            {
                MessageBox.Show("Enter SKU.");
                txtSKU.Focus();
                return false;
            }

            decimal price;

            if (!decimal.TryParse(txtPrice.Text, out price) || price < 0)
            {
                MessageBox.Show("Enter a valid price.");
                txtPrice.Focus();
                return false;
            }

            int stock;

            if (!int.TryParse(txtStockQty.Text, out stock) || stock < 0)
            {
                MessageBox.Show("Enter a valid stock quantity.");
                txtStockQty.Focus();
                return false;
            }

            if (cmbProductStatus.SelectedIndex == -1)
            {
                MessageBox.Show("Select a product status.");
                cmbProductStatus.Focus();
                return false;
            }

            return true;
        }

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            if (!ValidateProduct())
                return;

            try
            {
                using (SqlConnection connection = database.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        INSERT INTO Products
                        (
                            ProductName,
                            SKU,
                            Price,
                            StockQty,
                            [Status],
                            ProductDate,
                            LastUpdated
                        )
                        VALUES
                        (
                            @ProductName,
                            @SKU,
                            @Price,
                            @StockQty,
                            @Status,
                            @ProductDate,
                            GETDATE()
                        )";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductName",
                            txtProductName.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@SKU",
                            txtSKU.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@Price",
                            decimal.Parse(txtPrice.Text));

                        command.Parameters.AddWithValue(
                            "@StockQty",
                            int.Parse(txtStockQty.Text));

                        command.Parameters.AddWithValue(
                            "@Status",
                            cmbProductStatus.Text);

                        command.Parameters.AddWithValue(
                            "@ProductDate",
                            dtpProductDate.Value.Date);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product added successfully.");

                ClearProductFields();
                LoadDashboard();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnUpdateProduct_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductID.Text))
            {
                MessageBox.Show("Select a product first.");
                return;
            }

            if (!ValidateProduct())
                return;

            try
            {
                using (SqlConnection connection = database.GetConnection())
                {
                    connection.Open();

                    string query = @"
                        UPDATE Products
                        SET
                            ProductName = @ProductName,
                            SKU = @SKU,
                            Price = @Price,
                            StockQty = @StockQty,
                            [Status] = @Status,
                            ProductDate = @ProductDate,
                            LastUpdated = GETDATE()
                        WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            int.Parse(txtProductID.Text));

                        command.Parameters.AddWithValue(
                            "@ProductName",
                            txtProductName.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@SKU",
                            txtSKU.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@Price",
                            decimal.Parse(txtPrice.Text));

                        command.Parameters.AddWithValue(
                            "@StockQty",
                            int.Parse(txtStockQty.Text));

                        command.Parameters.AddWithValue(
                            "@Status",
                            cmbProductStatus.Text);

                        command.Parameters.AddWithValue(
                            "@ProductDate",
                            dtpProductDate.Value.Date);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product updated successfully.");

                ClearProductFields();
                LoadDashboard();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtProductID.Text))
            {
                MessageBox.Show("Select a product first.");
                return;
            }

            DialogResult result = MessageBox.Show(
                "Are you sure you want to delete this product?",
                "Delete Product",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection connection = database.GetConnection())
                {
                    connection.Open();

                    string query =
                        "DELETE FROM Products WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            int.Parse(txtProductID.Text));

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product deleted successfully.");

                ClearProductFields();
                LoadDashboard();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "This product cannot be deleted because it is already used in a sale.\n\n" +
                    ex.Message,
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void btnRefreshProduct_Click(object sender, EventArgs e)
        {
            ClearProductFields();
            LoadDashboard();
        }

        private void dgvProducts_CellClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];

            txtProductID.Text =
                row.Cells["ProductID"].Value?.ToString();

            txtProductName.Text =
                row.Cells["ProductName"].Value?.ToString();

            txtSKU.Text =
                row.Cells["SKU"].Value?.ToString();

            txtPrice.Text =
                row.Cells["Price"].Value?.ToString();

            txtStockQty.Text =
                row.Cells["StockQty"].Value?.ToString();

            cmbProductStatus.Text =
                row.Cells["Status"].Value?.ToString();

            if (row.Cells["ProductDate"].Value != null)
            {
                dtpProductDate.Value =
                    Convert.ToDateTime(row.Cells["ProductDate"].Value);
            }
        }

        private void ClearProductFields()
        {
            txtProductID.Clear();
            txtProductName.Clear();
            txtSKU.Clear();
            txtPrice.Clear();
            txtStockQty.Clear();
            cmbProductStatus.SelectedIndex = -1;
            dtpProductDate.Value = DateTime.Today;
        }
    }
}
       
