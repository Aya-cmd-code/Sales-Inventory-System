using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class InventoryUpdate : Form
    {
        private readonly DatabaseConnection database = new DatabaseConnection();
        private readonly bool canEdit;

        public InventoryUpdate() : this(true)
        {
        }

        public InventoryUpdate(bool canEdit)
        {
            InitializeComponent();
            this.canEdit = canEdit;

            txtCurrentStock.ReadOnly = true;

            cmbProduct.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAction.DropDownStyle = ComboBoxStyle.DropDownList;

            cmbAction.Items.Clear();
            cmbAction.Items.AddRange(new object[]
            {
                "Add Stock",
                "Remove Stock",
                "Set Stock"
            });

            nudQuantity.Minimum = 1;
            nudQuantity.Maximum = 1000000;

            cmbProduct.SelectedIndexChanged += cmbProduct_SelectedIndexChanged;
            btnUpdateStock.Click += btnUpdateStock_Click;

            SetInventoryPermissions();
            LoadProducts();
            LoadInventoryLogs();
        }

        private void SetInventoryPermissions()
        {
            btnUpdateStock.Enabled = canEdit;
            cmbAction.Enabled = canEdit;
            nudQuantity.Enabled = canEdit;
            txtReason.ReadOnly = !canEdit;
        }

        private void LoadProducts()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT ProductID, ProductName, StockQuantity
                    FROM Products
                    WHERE IsActive = 1
                    ORDER BY ProductName;", con))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    cmbProduct.DataSource = table;
                    cmbProduct.DisplayMember = "ProductName";
                    cmbProduct.ValueMember = "ProductID";
                    cmbProduct.SelectedIndex = -1;

                    txtCurrentStock.Clear();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load products.\n\n" + ex.Message,
                    "Inventory Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmbProduct_SelectedIndexChanged(object sender, EventArgs e)
        {
            DataRowView selected = cmbProduct.SelectedItem as DataRowView;

            txtCurrentStock.Text = selected == null
                ? string.Empty
                : Convert.ToString(selected["StockQuantity"]);
        }

        private void LoadInventoryLogs()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT TOP 200
                        LogID,
                        ProductName,
                        [Action],
                        QuantityChanged,
                        PreviousStock,
                        NewStock,
                        Reason,
                        UpdateDate
                    FROM InventoryLogs
                    ORDER BY UpdateDate DESC, LogID DESC;", con))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);

                    dgvInventoryLogs.DataSource = table;
                    dgvInventoryLogs.ReadOnly = true;
                    dgvInventoryLogs.AllowUserToAddRows = false;
                    dgvInventoryLogs.AutoSizeColumnsMode =
                        DataGridViewAutoSizeColumnsMode.Fill;
                    dgvInventoryLogs.SelectionMode =
                        DataGridViewSelectionMode.FullRowSelect;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load inventory logs.\n\n" + ex.Message,
                    "Inventory Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdateStock_Click(object sender, EventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("You are not authorized to update inventory.");
                return;
            }

            DataRowView selected = cmbProduct.SelectedItem as DataRowView;

            if (selected == null)
            {
                MessageBox.Show("Select a product.");
                return;
            }

            if (cmbAction.SelectedIndex < 0)
            {
                MessageBox.Show("Select an inventory action.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                MessageBox.Show("Enter a reason for this stock update.");
                txtReason.Focus();
                return;
            }

            int productId = Convert.ToInt32(selected["ProductID"]);
            int currentStock = Convert.ToInt32(selected["StockQuantity"]);
            int quantity = Convert.ToInt32(nudQuantity.Value);
            int newStock;

            string action = cmbAction.Text;

            try
            {
                if (action == "Add Stock")
                    newStock = checked(currentStock + quantity);
                else if (action == "Remove Stock")
                    newStock = currentStock - quantity;
                else
                    newStock = quantity;
            }
            catch (OverflowException)
            {
                MessageBox.Show("The stock quantity is too large.");
                return;
            }

            if (newStock < 0)
            {
                MessageBox.Show("You cannot remove more stock than is currently available.");
                return;
            }

            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    con.Open();

                    using (SqlTransaction transaction = con.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand command = new SqlCommand(@"
                                UPDATE Products
                                SET StockQuantity = @Stock,
                                    Status = CASE
                                        WHEN @Stock = 0 THEN 'Out of Stock'
                                        WHEN @Stock <= ReorderLevel THEN 'Low Stock'
                                        ELSE 'Available'
                                    END
                                WHERE ProductID = @ID
                                AND IsActive = 1;", con, transaction))
                            {
                                command.Parameters.AddWithValue("@Stock", newStock);
                                command.Parameters.AddWithValue("@ID", productId);

                                if (command.ExecuteNonQuery() != 1)
                                    throw new Exception("The selected product could not be updated.");
                            }

                            int loggedQuantity = Math.Abs(newStock - currentStock);

                            using (SqlCommand command = new SqlCommand(@"
                                INSERT INTO InventoryLogs
                                (
                                    ProductID,
                                    ProductName,
                                    [Action],
                                    QuantityChanged,
                                    PreviousStock,
                                    NewStock,
                                    Reason
                                )
                                SELECT
                                    ProductID,
                                    ProductName,
                                    @Action,
                                    @Quantity,
                                    @PreviousStock,
                                    @NewStock,
                                    @Reason
                                FROM Products
                                WHERE ProductID = @ID;", con, transaction))
                            {
                                command.Parameters.AddWithValue("@ID", productId);
                                command.Parameters.AddWithValue("@Action", action);
                                command.Parameters.AddWithValue("@Quantity", loggedQuantity);
                                command.Parameters.AddWithValue("@PreviousStock", currentStock);
                                command.Parameters.AddWithValue("@NewStock", newStock);
                                command.Parameters.AddWithValue("@Reason", txtReason.Text.Trim());

                                command.ExecuteNonQuery();
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            try
                            {
                                transaction.Rollback();
                            }
                            catch
                            {
                            }

                            throw;
                        }
                    }
                }

                MessageBox.Show("Inventory updated successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtReason.Clear();
                LoadProducts();
                LoadInventoryLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Inventory update failed.\n\n" + ex.Message,
                    "Inventory Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InventoryUpdate_Load(object sender, EventArgs e)
        {
        }
    }
}