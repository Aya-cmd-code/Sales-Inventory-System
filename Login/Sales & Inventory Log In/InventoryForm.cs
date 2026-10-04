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
    public partial class InventoryForm : Form

    {
        private DatabaseConnection database;
        public InventoryForm()
        {
            InitializeComponent();

            database = new DatabaseConnection();

            cmbStatus.Items.Clear();
            cmbStatus.Items.Add("In Stock");
            cmbStatus.Items.Add("Low Stock");
            cmbStatus.Items.Add("Out of Stock");

            txtProductID.ReadOnly = true;

            btnAddProduct.Click -= btnAddProduct_Click;
            btnAddProduct.Click += btnAddProduct_Click;

            btnUpdateProduct.Click -= btnUpdateProduct_Click;
            btnUpdateProduct.Click += btnUpdateProduct_Click;

            btnDeleteProduct.Click -= btnDeleteProduct_Click;
            btnDeleteProduct.Click += btnDeleteProduct_Click;

            btnRefreshProduct.Click -= btnRefreshProduct_Click;
            btnRefreshProduct.Click += btnRefreshProduct_Click;

            dgvInventory.CellClick -= dgvInventory_CellClick;
            dgvInventory.CellClick += dgvInventory_CellClick;

            LoadInventoryDashboard();
        }

        private void LoadInventoryDashboard()
        {
            try
            {
                LoadStatistics();
                LoadInventory();
                LoadStockLevels();
                LoadRecentInventoryUpdates();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error loading inventory dashboard:\n\n" +
                    ex.Message,
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
                        COUNT(*),
                        SUM(CASE WHEN StockQty > 0 AND StockQty <= 10 THEN 1 ELSE 0 END),
                        SUM(CASE WHEN StockQty = 0 THEN 1 ELSE 0 END),
                        ISNULL(SUM(Price * StockQty), 0)
                    FROM Products";

                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            lblTotalProducts.Text =
                                Convert.ToInt32(reader[0]).ToString();

                            lblLowStockItems.Text =
                                Convert.ToInt32(reader[1]).ToString();

                            lblOutOfStocks.Text =
                                Convert.ToInt32(reader[2]).ToString();

                            lblTotalStockValue.Text =
                                "₱" +
                                Convert.ToDecimal(reader[3]).ToString("N2");
                        }
                    }
                }
            }
        }

        private void LoadInventory()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        ProductID,
                        ProductName,
                        SKU,
                        StockQty,
                        [Status],
                        Price,
                        LastUpdated
                    FROM Products
                    ORDER BY ProductID DESC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    dgvInventory.DataSource = table;
                }
            }

            dgvInventory.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadStockLevels()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT
                        ProductName,
                        SKU,
                        StockQty,
                        [Status]
                    FROM Products
                    ORDER BY StockQty ASC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    dgvStockLevels.DataSource = table;
                }
            }

            dgvStockLevels.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void LoadRecentInventoryUpdates()
        {
            using (SqlConnection connection = database.GetConnection())
            {
                connection.Open();

                string query = @"
                    SELECT TOP 20
                        ProductName,
                        ChangeQty,
                        UpdateType,
                        UpdateDate
                    FROM InventoryUpdates
                    ORDER BY UpdateDate DESC";

                using (SqlDataAdapter adapter =
                    new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    dgvRecentInventory.DataSource = table;
                }
            }

            dgvRecentInventory.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;
        }

        private bool ValidateFields()
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

            int stock;

            if (!int.TryParse(txtStockQty.Text, out stock) ||
                stock < 0)
            {
                MessageBox.Show("Enter a valid stock quantity.");
                txtStockQty.Focus();
                return false;
            }

            if (cmbStatus.SelectedIndex == -1)
            {
                MessageBox.Show("Select a status.");
                cmbStatus.Focus();
                return false;
            }

            return true;
        }

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            if (!ValidateFields())
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
                            0,
                            @StockQty,
                            @Status,
                            GETDATE(),
                            GETDATE()
                        );

                        SELECT SCOPE_IDENTITY();";

                    int productID;

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
                            "@StockQty",
                            int.Parse(txtStockQty.Text));

                        command.Parameters.AddWithValue(
                            "@Status",
                            cmbStatus.Text);

                        productID =
                            Convert.ToInt32(
                                command.ExecuteScalar());
                    }

                    string logQuery = @"
                        INSERT INTO InventoryUpdates
                        (
                            ProductID,
                            ProductName,
                            ChangeQty,
                            UpdateType
                        )
                        VALUES
                        (
                            @ProductID,
                            @ProductName,
                            @ChangeQty,
                            'Added'
                        )";

                    using (SqlCommand command =
                        new SqlCommand(logQuery, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.Parameters.AddWithValue(
                            "@ProductName",
                            txtProductName.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@ChangeQty",
                            int.Parse(txtStockQty.Text));

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product added successfully.");

                LoadInventoryDashboard();
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

            if (!ValidateFields())
                return;

            int productID =
                int.Parse(txtProductID.Text);

            int newStock =
                int.Parse(txtStockQty.Text);

            try
            {
                using (SqlConnection connection = database.GetConnection())
                {
                    connection.Open();

                    int oldStock = 0;

                    string oldQuery = @"
                        SELECT StockQty
                        FROM Products
                        WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(oldQuery, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        object result =
                            command.ExecuteScalar();

                        if (result == null)
                        {
                            MessageBox.Show("Product not found.");
                            return;
                        }

                        oldStock = Convert.ToInt32(result);
                    }

                    string updateQuery = @"
                        UPDATE Products
                        SET
                            ProductName = @ProductName,
                            SKU = @SKU,
                            StockQty = @StockQty,
                            [Status] = @Status,
                            LastUpdated = GETDATE()
                        WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductName",
                            txtProductName.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@SKU",
                            txtSKU.Text.Trim());

                        command.Parameters.AddWithValue(
                            "@StockQty",
                            newStock);

                        command.Parameters.AddWithValue(
                            "@Status",
                            cmbStatus.Text);

                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.ExecuteNonQuery();
                    }

                    int difference =
                        newStock - oldStock;

                    if (difference != 0)
                    {
                        string logQuery = @"
                            INSERT INTO InventoryUpdates
                            (
                                ProductID,
                                ProductName,
                                ChangeQty,
                                UpdateType
                            )
                            VALUES
                            (
                                @ProductID,
                                @ProductName,
                                @ChangeQty,
                                'Updated'
                            )";

                        using (SqlCommand command =
                            new SqlCommand(logQuery, connection))
                        {
                            command.Parameters.AddWithValue(
                                "@ProductID",
                                productID);

                            command.Parameters.AddWithValue(
                                "@ProductName",
                                txtProductName.Text.Trim());

                            command.Parameters.AddWithValue(
                                "@ChangeQty",
                                difference);

                            command.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Product updated successfully.");

                LoadInventoryDashboard();
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

            int productID =
                int.Parse(txtProductID.Text);

            int stock =
                int.Parse(txtStockQty.Text);

            string productName =
                txtProductName.Text.Trim();

            try
            {
                using (SqlConnection connection = database.GetConnection())
                {
                    connection.Open();

                    string logQuery = @"
                        INSERT INTO InventoryUpdates
                        (
                            ProductID,
                            ProductName,
                            ChangeQty,
                            UpdateType
                        )
                        VALUES
                        (
                            @ProductID,
                            @ProductName,
                            @ChangeQty,
                            'Deleted'
                        )";

                    using (SqlCommand command =
                        new SqlCommand(logQuery, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.Parameters.AddWithValue(
                            "@ProductName",
                            productName);

                        command.Parameters.AddWithValue(
                            "@ChangeQty",
                            -stock);

                        command.ExecuteNonQuery();
                    }

                    string deleteQuery = @"
                        DELETE FROM Products
                        WHERE ProductID = @ProductID";

                    using (SqlCommand command =
                        new SqlCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@ProductID",
                            productID);

                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product deleted successfully.");

                LoadInventoryDashboard();
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
            LoadInventoryDashboard();
        }

        private void dgvInventory_CellClick(
            object sender,
            DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row =
                dgvInventory.Rows[e.RowIndex];

            txtProductID.Text =
                row.Cells["ProductID"].Value?.ToString();

            txtProductName.Text =
                row.Cells["ProductName"].Value?.ToString();

            txtSKU.Text =
                row.Cells["SKU"].Value?.ToString();

            txtStockQty.Text =
                row.Cells["StockQty"].Value?.ToString();

            cmbStatus.Text =
                row.Cells["Status"].Value?.ToString();
        }

        private void ClearFields()
        {
            txtProductID.Clear();
            txtProductName.Clear();
            txtSKU.Clear();
            txtStockQty.Clear();
            cmbStatus.SelectedIndex = -1;
        }
    }
}