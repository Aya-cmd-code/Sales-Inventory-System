using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class Payment_Form : Form
    {
        private readonly DatabaseConnection database = new DatabaseConnection();
        private int saleId;
        private decimal totalAmount;
        private decimal previouslyPaid;
        private readonly PrintDocument receiptDocument = new PrintDocument();
        private readonly bool canEdit;

        public Payment_Form() : this(true)
        {
        }

        public Payment_Form(bool canEdit)
        {
            InitializeComponent();
            this.canEdit = canEdit;
            SetupPaymentForm();
            LoadLatestUnpaidSale();
        }

        public Payment_Form(int selectedSaleId) : this(selectedSaleId, true)
        {
        }

        public Payment_Form(int selectedSaleId, bool canEdit)
        {
            InitializeComponent();
            this.canEdit = canEdit;
            SetupPaymentForm();
            saleId = selectedSaleId;
            LoadSale();
        }

        private void SetupPaymentForm()
        {
            txtInvoiceNumber.ReadOnly = true;
            txtCustomerName.ReadOnly = true;
            txtTotalAmount.ReadOnly = true;
            txtChange.ReadOnly = true;

            cmbPaymentMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPaymentMethod.Items.Clear();
            cmbPaymentMethod.Items.AddRange(new object[]
            {
                "Cash", "GCash", "Bank Transfer", "Card"
            });

            btnProcessPayment.Click += btnProcessPayment_Click;
            btnCancelPayment.Click += btnCancelPayment_Click;
            btnPrintReceipt.Click += btnPrintReceipt_Click;

            SetPaymentPermissions();

            dgvPaymentDetails.ReadOnly = true;
            dgvPaymentDetails.AllowUserToAddRows = false;
            dgvPaymentDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPaymentDetails.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            receiptDocument.PrintPage += receiptDocument_PrintPage;
            printPreviewReceipt.Document = receiptDocument;

            txtChange.Text = "Select an unpaid invoice.";
        }

        private void SetPaymentPermissions()
        {
            btnProcessPayment.Enabled = canEdit;
            cmbPaymentMethod.Enabled = canEdit;
            txtAmountReceived.ReadOnly = !canEdit;
        }

        private void LoadLatestUnpaidSale()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1 SaleID
                    FROM Sales
                    WHERE PaymentStatus <> 'Paid'
                    ORDER BY SaleDate DESC, SaleID DESC;", con))
                {
                    con.Open();
                    object result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        txtChange.Text = "No unpaid invoices found.";
                        return;
                    }

                    saleId = Convert.ToInt32(result);
                }

                LoadSale();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to find an unpaid invoice.\n\n" + ex.Message,
                    "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSale()
        {
            if (saleId <= 0)
                return;

            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT InvoiceNumber, CustomerName, TotalAmount
                    FROM Sales
                    WHERE SaleID = @ID;", con))
                {
                    cmd.Parameters.AddWithValue("@ID", saleId);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            MessageBox.Show("The selected sale could not be found.");
                            saleId = 0;
                            return;
                        }

                        txtInvoiceNumber.Text = Convert.ToString(reader["InvoiceNumber"]);
                        txtCustomerName.Text = Convert.ToString(reader["CustomerName"]);
                        totalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                        txtTotalAmount.Text = totalAmount.ToString("N2");
                    }
                }

                LoadPaymentHistory();
                CalculateBalance();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load sale details.\n\n" + ex.Message,
                    "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPaymentHistory()
        {
            using (SqlConnection con = database.GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT PaymentMethod, AmountReceived, AmountPaid,
                           ChangeAmount, PaymentDate
                    FROM Payments
                    WHERE SaleID = @ID
                    ORDER BY PaymentDate DESC;", con))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@ID", saleId);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvPaymentDetails.DataSource = table;
                }

                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT ISNULL(SUM(AmountPaid), 0)
                    FROM Payments
                    WHERE SaleID = @ID;", con))
                {
                    cmd.Parameters.AddWithValue("@ID", saleId);
                    con.Open();
                    previouslyPaid = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        private decimal GetBalance()
        {
            return Math.Max(0m, totalAmount - previouslyPaid);
        }

        private void CalculateBalance()
        {
            txtChange.Text = "Remaining balance: ₱" + GetBalance().ToString("N2");
        }

        private void btnProcessPayment_Click(object sender, EventArgs e)
        {
            if (!canEdit)
            {
                MessageBox.Show("You are not authorized to process payments.");
                return;
            }

            if (saleId <= 0)
            {
                MessageBox.Show("There is no invoice selected.");
                return;
            }

            if (cmbPaymentMethod.SelectedIndex < 0)
            {
                MessageBox.Show("Select a payment method.");
                return;
            }

            decimal received;

            if (!decimal.TryParse(txtAmountReceived.Text.Trim(), out received) || received <= 0)
            {
                MessageBox.Show("Enter a valid amount received.");
                txtAmountReceived.Focus();
                return;
            }

            decimal balance = GetBalance();

            if (balance <= 0)
            {
                MessageBox.Show("This invoice is already fully paid.");
                return;
            }

            decimal applied = Math.Min(received, balance);
            decimal change = received - applied;

            using (SqlConnection con = database.GetConnection())
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        bool stockDeducted;

                        using (SqlCommand command = new SqlCommand(@"
                            SELECT StockDeducted
                            FROM Sales WITH (UPDLOCK, HOLDLOCK)
                            WHERE SaleID = @ID;", con, transaction))
                        {
                            command.Parameters.AddWithValue("@ID", saleId);
                            object result = command.ExecuteScalar();

                            if (result == null || result == DBNull.Value)
                                throw new Exception("Unable to find the sale stock status.");

                            stockDeducted = Convert.ToBoolean(result);
                        }

                        if (!stockDeducted)
                        {
                            DataTable items = new DataTable();

                            using (SqlCommand command = new SqlCommand(@"
                                SELECT ProductID, Quantity
                                FROM SaleItems
                                WHERE SaleID = @ID;", con, transaction))
                            {
                                command.Parameters.AddWithValue("@ID", saleId);

                                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                                {
                                    adapter.Fill(items);
                                }
                            }

                            if (items.Rows.Count == 0)
                                throw new Exception("No sale items were found for this invoice.");

                            foreach (DataRow item in items.Rows)
                            {
                                int productId = Convert.ToInt32(item["ProductID"]);
                                int quantity = Convert.ToInt32(item["Quantity"]);
                                int previousStock;

                                using (SqlCommand command = new SqlCommand(@"
                                    SELECT StockQuantity
                                    FROM Products WITH (UPDLOCK, HOLDLOCK)
                                    WHERE ProductID = @ID
                                    AND IsActive = 1;", con, transaction))
                                {
                                    command.Parameters.AddWithValue("@ID", productId);
                                    object result = command.ExecuteScalar();

                                    if (result == null || result == DBNull.Value)
                                        throw new Exception("A product is missing or inactive.");

                                    previousStock = Convert.ToInt32(result);
                                }

                                if (previousStock < quantity)
                                    throw new Exception("Insufficient stock for Product ID " + productId);

                                int newStock = previousStock - quantity;

                                using (SqlCommand command = new SqlCommand(@"
                                    UPDATE Products
                                    SET StockQuantity = @Stock,
                                        Status = CASE
                                            WHEN @Stock = 0 THEN 'Out of Stock'
                                            WHEN @Stock <= ReorderLevel THEN 'Low Stock'
                                            ELSE 'Available'
                                        END
                                    WHERE ProductID = @ID;", con, transaction))
                                {
                                    command.Parameters.AddWithValue("@Stock", newStock);
                                    command.Parameters.AddWithValue("@ID", productId);
                                    command.ExecuteNonQuery();
                                }

                                using (SqlCommand command = new SqlCommand(@"
                                    INSERT INTO InventoryLogs
                                    (ProductID, ProductName, [Action],
                                     QuantityChanged, PreviousStock, NewStock, Reason)
                                    SELECT ProductID, ProductName, 'Sale',
                                           @Quantity, @Previous, @New, @Reason
                                    FROM Products
                                    WHERE ProductID = @ID;", con, transaction))
                                {
                                    command.Parameters.AddWithValue("@ID", productId);
                                    command.Parameters.AddWithValue("@Quantity", quantity);
                                    command.Parameters.AddWithValue("@Previous", previousStock);
                                    command.Parameters.AddWithValue("@New", newStock);
                                    command.Parameters.AddWithValue("@Reason",
                                        "Sale invoice " + txtInvoiceNumber.Text);

                                    command.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand command = new SqlCommand(@"
                                UPDATE Sales
                                SET StockDeducted = 1
                                WHERE SaleID = @ID;", con, transaction))
                            {
                                command.Parameters.AddWithValue("@ID", saleId);
                                command.ExecuteNonQuery();
                            }
                        }

                        string status = previouslyPaid + applied >= totalAmount
                            ? "Paid" : "Partially Paid";

                        using (SqlCommand command = new SqlCommand(@"
                            INSERT INTO Payments
                            (SaleID, InvoiceNo, CustomerName, TotalAmount,
                             PaymentMethod, AmountReceived, AmountPaid,
                             ChangeAmount, PaymentStatus)
                            VALUES
                            (@SaleID, @InvoiceNo, @Customer, @Total,
                             @Method, @Received, @Applied,
                             @Change, @Status);", con, transaction))
                        {
                            command.Parameters.AddWithValue("@SaleID", saleId);
                            command.Parameters.AddWithValue("@InvoiceNo", txtInvoiceNumber.Text);
                            command.Parameters.AddWithValue("@Customer", txtCustomerName.Text);
                            command.Parameters.AddWithValue("@Total", totalAmount);
                            command.Parameters.AddWithValue("@Method", cmbPaymentMethod.Text);
                            command.Parameters.AddWithValue("@Received", received);
                            command.Parameters.AddWithValue("@Applied", applied);
                            command.Parameters.AddWithValue("@Change", change);
                            command.Parameters.AddWithValue("@Status", status);
                            command.ExecuteNonQuery();
                        }

                        using (SqlCommand command = new SqlCommand(@"
                            UPDATE Sales
                            SET PaymentStatus = @Status
                            WHERE SaleID = @ID;", con, transaction))
                        {
                            command.Parameters.AddWithValue("@Status", status);
                            command.Parameters.AddWithValue("@ID", saleId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show(
                            "Payment recorded successfully.\nAmount applied: ₱" +
                            applied.ToString("N2") +
                            "\nChange: ₱" + change.ToString("N2"),
                            "Payment Successful",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        LoadSale();
                        txtAmountReceived.Clear();
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
                            "Payment failed. No payment or stock changes were committed.\n\n" + ex.Message,
                            "Payment Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnCancelPayment_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (saleId <= 0 || string.IsNullOrWhiteSpace(txtInvoiceNumber.Text))
            {
                MessageBox.Show("There is no invoice to print.");
                return;
            }

            printPreviewReceipt.ShowDialog(this);
        }

        private void receiptDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            float y = 40;

            using (Font heading = new Font("Arial", 16, FontStyle.Bold))
            using (Font normal = new Font("Arial", 10))
            {
                e.Graphics.DrawString("SALES AND INVENTORY SYSTEM", heading, Brushes.Black, 40, y);
                y += 40;
                e.Graphics.DrawString("PAYMENT RECEIPT", heading, Brushes.Black, 40, y);
                y += 45;
                e.Graphics.DrawString("Invoice: " + txtInvoiceNumber.Text, normal, Brushes.Black, 40, y);
                y += 25;
                e.Graphics.DrawString("Customer: " + txtCustomerName.Text, normal, Brushes.Black, 40, y);
                y += 25;
                e.Graphics.DrawString("Total Amount: ₱" + totalAmount.ToString("N2"), normal, Brushes.Black, 40, y);
                y += 25;
                e.Graphics.DrawString("Total Paid: ₱" + previouslyPaid.ToString("N2"), normal, Brushes.Black, 40, y);
                y += 25;
                e.Graphics.DrawString("Balance: ₱" + GetBalance().ToString("N2"), normal, Brushes.Black, 40, y);
                y += 35;
                e.Graphics.DrawString("Thank you for your purchase!", normal, Brushes.Black, 40, y);
            }

            e.HasMorePages = false;
        }

        private void Payment_Form_Load(object sender, EventArgs e)
        {
        }
    }
}