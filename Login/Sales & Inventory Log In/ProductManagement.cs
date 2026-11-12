using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class ProductManagement : Form
    {
        private readonly DatabaseConnection database = new DatabaseConnection();

        public ProductManagement()
        {
            InitializeComponent();
            txtProductID.ReadOnly = true;
            cmbCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProductStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCategory.Items.Clear();
            cmbCategory.Items.AddRange(new object[] { "School Supplies", "Electronics", "Food", "Accessories", "Others" });
            cmbProductStatus.Items.Clear();
            cmbProductStatus.Items.AddRange(new object[] { "Available", "Low Stock", "Out of Stock" });
            cmbProductStatus.Enabled = false;

            dgvProducts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProducts.MultiSelect = false;
            dgvProducts.ReadOnly = true;
            dgvProducts.AllowUserToAddRows = false;
            dgvProducts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            btnAddProduct.Click += btnAddProduct_Click;
            btnUpdateProduct.Click += btnUpdateProduct_Click;
            btnDeleteProduct.Click += btnDeleteProduct_Click;
            btnClearProduct.Click += btnClearProduct_Click;
            btnRefreshProducts.Click += btnRefreshProducts_Click;
            btnSearchProduct.Click += btnSearchProduct_Click;
            dgvProducts.CellClick += dgvProducts_CellClick;
            txtStockQuantity.TextChanged += ProductFields_TextChanged;
            txtReorderLevel.TextChanged += ProductFields_TextChanged;

            LoadProducts();
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT ProductID, ProductName, Category, Price, StockQuantity, ReorderLevel, Status
                    FROM Products WHERE IsActive = 1 ORDER BY ProductName;", con))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvProducts.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load products.\n\n" + ex.Message, "Product Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddProduct_Click(object sender, EventArgs e)
        {
            if (!ValidateFields()) return;
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlCommand command = new SqlCommand(@"
                    INSERT INTO Products (ProductName, Category, Price, StockQuantity, ReorderLevel, Status, IsActive)
                    VALUES (@Name, @Category, @Price, @Stock, @Reorder, @Status, 1);", con))
                {
                    AddParameters(command);
                    con.Open();
                    command.ExecuteNonQuery();
                }
                MessageBox.Show("Product added successfully.");
                LoadProducts();
                ClearFields();
            }
            catch (Exception ex) { MessageBox.Show("Unable to add product.\n\n" + ex.Message); }
        }

        private void btnUpdateProduct_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(txtProductID.Text, out id))
            {
                MessageBox.Show("Select a product to update.");
                return;
            }
            if (!ValidateFields()) return;
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlCommand command = new SqlCommand(@"
                    UPDATE Products SET ProductName=@Name, Category=@Category, Price=@Price,
                        StockQuantity=@Stock, ReorderLevel=@Reorder, Status=@Status
                    WHERE ProductID=@ID AND IsActive=1;", con))
                {
                    AddParameters(command);
                    command.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    if (command.ExecuteNonQuery() != 1)
                        throw new Exception("Product was not found or is inactive.");
                }
                MessageBox.Show("Product updated successfully.");
                LoadProducts();
                ClearFields();
            }
            catch (Exception ex) { MessageBox.Show("Unable to update product.\n\n" + ex.Message); }
        }

        private void btnDeleteProduct_Click(object sender, EventArgs e)
        {
            int id;
            if (!int.TryParse(txtProductID.Text, out id))
            {
                MessageBox.Show("Select a product to delete.");
                return;
            }
            if (MessageBox.Show("Are you sure you want to deactivate this product? Existing sales records will be preserved.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlCommand command = new SqlCommand("UPDATE Products SET IsActive=0 WHERE ProductID=@ID;", con))
                {
                    command.Parameters.AddWithValue("@ID", id);
                    con.Open();
                    command.ExecuteNonQuery();
                }
                MessageBox.Show("Product removed from the active product list.");
                LoadProducts();
                ClearFields();
            }
            catch (Exception ex) { MessageBox.Show("Unable to delete product.\n\n" + ex.Message); }
        }

        private void btnClearProduct_Click(object sender, EventArgs e) { ClearFields(); }
        private void btnRefreshProducts_Click(object sender, EventArgs e) { LoadProducts(); ClearFields(); }

        private void btnSearchProduct_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT ProductID, ProductName, Category, Price, StockQuantity, ReorderLevel, Status
                    FROM Products
                    WHERE IsActive=1 AND (ProductName LIKE @Search OR Category LIKE @Search)
                    ORDER BY ProductName;", con))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@Search", "%" + txtSearchProduct.Text.Trim() + "%");
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvProducts.DataSource = table;
                }
            }
            catch (Exception ex) { MessageBox.Show("Search failed.\n\n" + ex.Message); }
        }

        private void dgvProducts_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvProducts.Rows[e.RowIndex];
            txtProductID.Text = Convert.ToString(row.Cells["ProductID"].Value);
            txtProductName.Text = Convert.ToString(row.Cells["ProductName"].Value);
            cmbCategory.Text = Convert.ToString(row.Cells["Category"].Value);
            txtPrice.Text = Convert.ToString(row.Cells["Price"].Value);
            txtStockQuantity.Text = Convert.ToString(row.Cells["StockQuantity"].Value);
            txtReorderLevel.Text = Convert.ToString(row.Cells["ReorderLevel"].Value);
            UpdateStatusPreview();
        }

        private void ProductFields_TextChanged(object sender, EventArgs e) { UpdateStatusPreview(); }

        private void UpdateStatusPreview()
        {
            int stock, reorder;
            if (!int.TryParse(txtStockQuantity.Text, out stock) || !int.TryParse(txtReorderLevel.Text, out reorder))
                return;
            cmbProductStatus.Text = stock <= 0 ? "Out of Stock" : stock <= reorder ? "Low Stock" : "Available";
        }

        private bool ValidateFields()
        {
            decimal price;
            int stock, reorder;
            if (string.IsNullOrWhiteSpace(txtProductName.Text)) { MessageBox.Show("Enter product name."); return false; }
            if (cmbCategory.SelectedIndex < 0) { MessageBox.Show("Select a category."); return false; }
            if (!decimal.TryParse(txtPrice.Text, out price) || price <= 0) { MessageBox.Show("Enter a valid price greater than zero."); return false; }
            if (!int.TryParse(txtStockQuantity.Text, out stock) || stock < 0) { MessageBox.Show("Enter a valid stock quantity."); return false; }
            if (!int.TryParse(txtReorderLevel.Text, out reorder) || reorder < 0) { MessageBox.Show("Enter a valid reorder level."); return false; }
            UpdateStatusPreview();
            return true;
        }

        private void AddParameters(SqlCommand command)
        {
            int stock = Convert.ToInt32(txtStockQuantity.Text);
            int reorder = Convert.ToInt32(txtReorderLevel.Text);
            string status = stock <= 0 ? "Out of Stock" : stock <= reorder ? "Low Stock" : "Available";
            command.Parameters.AddWithValue("@Name", txtProductName.Text.Trim());
            command.Parameters.AddWithValue("@Category", cmbCategory.Text);
            command.Parameters.AddWithValue("@Price", Convert.ToDecimal(txtPrice.Text));
            command.Parameters.AddWithValue("@Stock", stock);
            command.Parameters.AddWithValue("@Reorder", reorder);
            command.Parameters.AddWithValue("@Status", status);
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
