
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class ProductManagement : Form
    {
        private DatabaseConnection database =
            new DatabaseConnection();

        public ProductManagement()
        {
            InitializeComponent();

            txtProductID.ReadOnly = true;

            cmbCategory.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbProductStatus.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbCategory.Items.AddRange(new string[]
            {
                "School Supplies",
                "Electronics",
                "Food",
                "Accessories",
                "Others"
            });

            cmbProductStatus.Items.AddRange(new string[]
            {
                "Available",
                "Low Stock",
                "Out of Stock"
            });

            dgvProducts.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvProducts.MultiSelect = false;
            dgvProducts.ReadOnly = true;
            dgvProducts.AllowUserToAddRows = false;

            dgvProducts.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            btnAddProduct.Click += btnAddProduct_Click;
            btnUpdateProduct.Click += btnUpdateProduct_Click;
            btnDeleteProduct.Click += btnDeleteProduct_Click;
            btnClearProduct.Click += btnClearProduct_Click;
            btnRefreshProducts.Click += btnRefreshProducts_Click;
            btnSearchProduct.Click += btnSearchProduct_Click;

            dgvProducts.CellClick += dgvProducts_CellClick;

            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    string query = @"
                        SELECT ProductID, ProductName, Category,
                        Price, StockQuantity, ReorderLevel, Status
                        FROM Products";

                    SqlDataAdapter adapter =
                        new SqlDataAdapter(query, con);

                    DataTable table = new DataTable();

                    adapter.Fill(table);

                    dgvProducts.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            if (!ValidateFields())
                return;

            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    con.Open();

                    string query = @"
                        INSERT INTO Products
                        (ProductName, Category, Price,
                        StockQuantity, ReorderLevel, Status)
                        VALUES
                        (@Name, @Category, @Price,
                        @Stock, @Reorder, @Status)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        AddParameters(cmd);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product added successfully!");

                LoadProducts();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnUpdateProduct_Click(object sender, EventArgs e)
        {
            if (txtProductID.Text == "")
            {
                MessageBox.Show("Please select a product.");
                return;
            }

            if (!ValidateFields())
                return;

            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    con.Open();

                    string query = @"
                        UPDATE Products SET
                        ProductName = @Name,
                        Category = @Category,
                        Price = @Price,
                        StockQuantity = @Stock,
                        ReorderLevel = @Reorder,
                        Status = @Status
                        WHERE ProductID = @ID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        AddParameters(cmd);

                        cmd.Parameters.AddWithValue(
                            "@ID", Convert.ToInt32(txtProductID.Text));

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product updated successfully!");

                LoadProducts();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            if (txtProductID.Text == "")
            {
                MessageBox.Show("Please select a product.");
                return;
            }

            if (MessageBox.Show(
                "Are you sure you want to delete this product?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    con.Open();

                    string query =
                        "DELETE FROM Products WHERE ProductID = @ID";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@ID", Convert.ToInt32(txtProductID.Text));

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product deleted successfully!");

                LoadProducts();
                ClearFields();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "This product may already have sales records.\n\n" +
                    ex.Message);
            }
        }

        private void btnClearProduct_Click(object sender, EventArgs e)
        {
            ClearFields();
        }

        private void btnRefreshProducts_Click(object sender, EventArgs e)
        {
            LoadProducts();
            ClearFields();
        }

        private void btnSearchProduct_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    string query = @"
                        SELECT * FROM Products
                        WHERE ProductName LIKE @Search
                        OR Category LIKE @Search";

                    SqlDataAdapter adapter =
                        new SqlDataAdapter(query, con);

                    adapter.SelectCommand.Parameters.AddWithValue(
                        "@Search", "%" + txtSearchProduct.Text.Trim() + "%");

                    DataTable table = new DataTable();

                    adapter.Fill(table);

                    dgvProducts.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgvProducts_CellClick(
            object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
                return;

            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];

            txtProductID.Text = row.Cells["ProductID"].Value.ToString();
            txtProductName.Text = row.Cells["ProductName"].Value.ToString();
            cmbCategory.Text = row.Cells["Category"].Value.ToString();
            txtPrice.Text = row.Cells["Price"].Value.ToString();
            txtStockQuantity.Text = row.Cells["StockQuantity"].Value.ToString();
            txtReorderLevel.Text = row.Cells["ReorderLevel"].Value.ToString();
            cmbProductStatus.Text = row.Cells["Status"].Value.ToString();
        }

        private bool ValidateFields()
        {
            decimal price;
            int stock;
            int reorder;

            if (string.IsNullOrWhiteSpace(txtProductName.Text))
            {
                MessageBox.Show("Enter product name.");
                return false;
            }

            if (cmbCategory.SelectedIndex == -1)
            {
                MessageBox.Show("Select category.");
                return false;
            }

            if (!decimal.TryParse(txtPrice.Text, out price) || price <= 0)
            {
                MessageBox.Show("Enter a valid price.");
                return false;
            }

            if (!int.TryParse(txtStockQuantity.Text, out stock) || stock < 0)
            {
                MessageBox.Show("Enter a valid stock quantity.");
                return false;
            }

            if (!int.TryParse(txtReorderLevel.Text, out reorder) || reorder < 0)
            {
                MessageBox.Show("Enter a valid reorder level.");
                return false;
            }

            if (cmbProductStatus.SelectedIndex == -1)
            {
                MessageBox.Show("Select a status.");
                return false;
            }

            return true;
        }

        private void AddParameters(SqlCommand cmd)
        {
            int stock = Convert.ToInt32(txtStockQuantity.Text);
            int reorder = Convert.ToInt32(txtReorderLevel.Text);

            string status;

            if (stock == 0)
                status = "Out of Stock";
            else if (stock <= reorder)
                status = "Low Stock";
            else
                status = "Available";

            cmd.Parameters.AddWithValue("@Name", txtProductName.Text.Trim());
            cmd.Parameters.AddWithValue("@Category", cmbCategory.Text);
            cmd.Parameters.AddWithValue("@Price", Convert.ToDecimal(txtPrice.Text));
            cmd.Parameters.AddWithValue("@Stock", stock);
            cmd.Parameters.AddWithValue("@Reorder", reorder);
            cmd.Parameters.AddWithValue("@Status", status);
        }

        private void ClearFields()
        {
            txtProductID.Clear();
            txtProductName.Clear();
            txtPrice.Clear();
            txtStockQuantity.Clear();
            txtReorderLevel.Clear();
            txtSearchProduct.Clear();

            cmbCategory.SelectedIndex = -1;
            cmbProductStatus.SelectedIndex = -1;

            dgvProducts.ClearSelection();
        }
    }
}
