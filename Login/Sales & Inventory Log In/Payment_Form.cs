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

        public Payment_Form()
        {
            InitializeComponent();
            SetupPaymentForm();
            LoadLatestUnpaidSale();
        }

        public Payment_Form(int selectedSaleId)
        {
            InitializeComponent();
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
            cmbPaymentMethod.Items.AddRange(new object[] { "Cash", "GCash", "Bank Transfer", "Card" });

            btnProcessPayment.Click += btnProcessPayment_Click;
            btnCancelPayment.Click += btnCancelPayment_Click;
            btnPrintReceipt.Click += btnPrintReceipt_Click;

            dgvPaymentDetails.ReadOnly = true;
            dgvPaymentDetails.AllowUserToAddRows = false;
            dgvPaymentDetails.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPaymentDetails.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            receiptDocument.PrintPage += receiptDocument_PrintPage;
            printPreviewReceipt.Document = receiptDocument;
            txtChange.Text = "Select an unpaid invoice.";
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
                MessageBox.Show("Unable to find an unpaid invoice.\n\n" + ex.Message, "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSale()
        {
            if (saleId <= 0) return;

            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlCommand cmd = new SqlCommand(@"
                    SELECT InvoiceNumber, CustomerName, TotalAmount
                    FROM Sales WHERE SaleID = @ID;", con))
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
                MessageBox.Show("Unable to load sale details.\n\n" + ex.Message, "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPaymentHistory()
        {
            using (SqlConnection con = database.GetConnection())
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT PaymentMethod, AmountReceived, AmountPaid, ChangeAmount, PaymentDate
                    FROM Payments WHERE SaleID = @ID ORDER BY PaymentDate DESC;", con))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@ID", saleId);
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    dgvPaymentDetails.DataSource = table;
                }

                using (SqlCommand cmd = new SqlCommand("SELECT ISNULL(SUM(AmountPaid), 0) FROM Payments WHERE SaleID = @ID;", con))
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
            if (saleId <= 0)
            {
                MessageBox.Show("There is no invoice selected. Create a sale first or select an unpaid invoice.");
                return;
            }
            if (cmbPaymentMethod.SelectedIndex < 0)
            {
                MessageBox.Show("Select a payment method.");
                return;
            }

            decimal received;
            if (!decimal.TryParse(txtAmountReceived.Text.Trim(), out received) || received <= 0m)
            {
                MessageBox.Show("Enter a valid amount received.");
                txtAmountReceived.Focus();
                return;
            }

            decimal balance = GetBalance();
            if (balance <= 0m)
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
                        using (SqlCommand command = new SqlCommand(@"
                            INSERT INTO Payments (SaleID, PaymentMethod, AmountReceived, AmountPaid, ChangeAmount)
                            VALUES (@SaleID, @Method, @Received, @Applied, @Change);", con, transaction))
                        {
                            command.Parameters.AddWithValue("@SaleID", saleId);
                            command.Parameters.AddWithValue("@Method", cmbPaymentMethod.Text);
                            command.Parameters.AddWithValue("@Received", received);
                            command.Parameters.AddWithValue("@Applied", applied);
                            command.Parameters.AddWithValue("@Change", change);
                            command.ExecuteNonQuery();
                        }

                        bool stockDeducted;
                        using (SqlCommand command = new SqlCommand("SELECT StockDeducted FROM Sales WITH (UPDLOCK, HOLDLOCK) WHERE SaleID = @ID;", con, transaction))
                        {
                            command.Parameters.AddWithValue("@ID", saleId);
                            object result = command.ExecuteScalar();
                            if (result == null || result == DBNull.Value)
                                throw new Exception("The sale record or StockDeducted value is missing. Run the supplied database setup script.");
                            stockDeducted = Convert.ToBoolean(result);
                        }

                        if (!stockDeducted)
                        {
                            DataTable items = new DataTable();
                            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT ProductID, Quantity FROM SaleItems WHERE SaleID = @ID;", con))
                            {
                                adapter.SelectCommand.Transaction = transaction;
                                adapter.SelectCommand.Parameters.AddWithValue("@ID", saleId);
                                adapter.Fill(items);
                            }

                            if (items.Rows.Count == 0)
                                throw new Exception("No sale items were found for this invoice.");

                            foreach (DataRow item in items.Rows)
                            {
                                using (SqlCommand command = new SqlCommand(@"
                                    UPDATE Products
                                    SET StockQuantity = StockQuantity - @Quantity,
                                        Status = CASE
                                            WHEN StockQuantity - @Quantity <= 0 THEN 'Out of Stock'
                                            WHEN StockQuantity - @Quantity <= ReorderLevel THEN 'Low Stock'
                                            ELSE 'Available' END
                                    WHERE ProductID = @ProductID AND IsActive = 1 AND StockQuantity >= @Quantity;", con, transaction))
                                {
                                    command.Parameters.AddWithValue("@Quantity", Convert.ToInt32(item["Quantity"]));
                                    command.Parameters.AddWithValue("@ProductID", Convert.ToInt32(item["ProductID"]));
                                    if (command.ExecuteNonQuery() != 1)
                                        throw new Exception("Insufficient stock or inactive product. Payment was not saved.");
                                }

                                using (SqlCommand command = new SqlCommand(@"
                                    INSERT INTO InventoryLogs (ProductID, [Action], Quantity, Reason)
                                    VALUES (@ProductID, 'Sale', @Quantity, @Reason);", con, transaction))
                                {
                                    command.Parameters.AddWithValue("@ProductID", Convert.ToInt32(item["ProductID"]));
                                    command.Parameters.AddWithValue("@Quantity", Convert.ToInt32(item["Quantity"]));
                                    command.Parameters.AddWithValue("@Reason", "Sale invoice " + txtInvoiceNumber.Text);
                                    command.ExecuteNonQuery();
                                }
                            }

                            using (SqlCommand command = new SqlCommand("UPDATE Sales SET StockDeducted = 1 WHERE SaleID = @ID;", con, transaction))
                            {
                                command.Parameters.AddWithValue("@ID", saleId);
                                command.ExecuteNonQuery();
                            }
                        }

                        decimal newPaid = previouslyPaid + applied;
                        string status = newPaid >= totalAmount ? "Paid" : "Partially Paid";
                        using (SqlCommand command = new SqlCommand("UPDATE Sales SET PaymentStatus = @Status WHERE SaleID = @ID;", con, transaction))
                        {
                            command.Parameters.AddWithValue("@Status", status);
                            command.Parameters.AddWithValue("@ID", saleId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        MessageBox.Show("Payment recorded successfully.\nAmount applied: ₱" + applied.ToString("N2") + "\nChange: ₱" + change.ToString("N2"), "Payment Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadSale();
                        txtAmountReceived.Clear();
                    }
                    catch (Exception ex)
                    {
                        try { transaction.Rollback(); } catch { }
                        MessageBox.Show("Payment failed. No payment or stock changes were committed.\n\n" + ex.Message, "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
    }
}
