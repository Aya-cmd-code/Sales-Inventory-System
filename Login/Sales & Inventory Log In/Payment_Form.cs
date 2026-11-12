
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
        private DatabaseConnection database = new DatabaseConnection();
        private int saleId = 0;
        private decimal totalAmount = 0;
        private decimal previouslyPaid = 0;
        private PrintDocument receiptDocument = new PrintDocument();

        public Payment_Form()
        {
            InitializeComponent();
            SetupPaymentForm();
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

            cmbPaymentMethod.Items.Clear();
            cmbPaymentMethod.Items.AddRange(new object[]
            {
                "Cash", "GCash", "Bank Transfer", "Card"
            });

            btnProcessPayment.Click += btnProcessPayment_Click;
            btnCancelPayment.Click += btnCancelPayment_Click;
            btnPrintReceipt.Click += btnPrintReceipt_Click;

            dgvPaymentDetails.ReadOnly = true;
            dgvPaymentDetails.AllowUserToAddRows = false;
            dgvPaymentDetails.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            receiptDocument.PrintPage += receiptDocument_PrintPage;
            printPreviewReceipt.Document = receiptDocument;
        }

        private void LoadSale()
        {
            using (SqlConnection con = database.GetConnection())
            {
                string query = @"
                    SELECT InvoiceNumber, CustomerName, TotalAmount
                    FROM Sales
                    WHERE SaleID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ID", saleId);
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            txtInvoiceNumber.Text = reader["InvoiceNumber"].ToString();
                            txtCustomerName.Text = reader["CustomerName"].ToString();
                            totalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                            txtTotalAmount.Text = totalAmount.ToString("N2");
                        }
                        else
                        {
                            MessageBox.Show("Sale not found.");
                            return;
                        }
                    }
                }
            }

            LoadPaymentHistory();
            CalculateBalance();
        }

        private void LoadPaymentHistory()
        {
            using (SqlConnection con = database.GetConnection())
            {
                string query = @"
                    SELECT PaymentMethod, AmountReceived, AmountPaid,
                           ChangeAmount, PaymentDate
                    FROM Payments
                    WHERE SaleID = @ID
                    ORDER BY PaymentDate";

                SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                adapter.SelectCommand.Parameters.AddWithValue("@ID", saleId);

                DataTable table = new DataTable();
                adapter.Fill(table);
                dgvPaymentDetails.DataSource = table;

                string sumQuery = @"
                    SELECT ISNULL(SUM(AmountPaid), 0)
                    FROM Payments WHERE SaleID = @ID";

                using (SqlCommand cmd = new SqlCommand(sumQuery, con))
                {
                    cmd.Parameters.AddWithValue("@ID", saleId);
                    con.Open();
                    previouslyPaid = Convert.ToDecimal(cmd.ExecuteScalar());
                }
            }
        }

        private decimal GetBalance()
        {
            return Math.Max(0, totalAmount - previouslyPaid);
        }

        private void CalculateBalance()
        {
            txtChange.Text = "Remaining balance: ₱" +
                GetBalance().ToString("N2");
        }

        private void btnProcessPayment_Click(object sender, EventArgs e)
        {
            if (saleId <= 0)
            {
                MessageBox.Show("Open a sale from the Sales Transaction form first.");
                return;
            }

            if (cmbPaymentMethod.SelectedIndex < 0)
            {
                MessageBox.Show("Select a payment method.");
                return;
            }

            decimal received;

            if (!decimal.TryParse(txtAmountReceived.Text, out received) ||
                received <= 0)
            {
                MessageBox.Show("Enter a valid amount received.");
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
                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    string paymentQuery = @"
                        INSERT INTO Payments
                        (SaleID, PaymentMethod, AmountReceived,
                         AmountPaid, ChangeAmount)
                        VALUES
                        (@SaleID, @Method, @Received, @Applied, @Change)";

                    using (SqlCommand cmd = new SqlCommand(paymentQuery, con, trans))
                    {
                        cmd.Parameters.AddWithValue("@SaleID", saleId);
                        cmd.Parameters.AddWithValue("@Method", cmbPaymentMethod.Text);
                        cmd.Parameters.AddWithValue("@Received", received);
                        cmd.Parameters.AddWithValue("@Applied", applied);
                        cmd.Parameters.AddWithValue("@Change", change);
                        cmd.ExecuteNonQuery();
                    }

                    string stockCheck = @"
                        SELECT StockDeducted
                        FROM Sales WITH (UPDLOCK, HOLDLOCK)
                        WHERE SaleID = @ID";

                    bool stockDeducted;

                    using (SqlCommand cmd = new SqlCommand(stockCheck, con, trans))
                    {
                        cmd.Parameters.AddWithValue("@ID", saleId);
                        stockDeducted = Convert.ToBoolean(cmd.ExecuteScalar());
                    }

                    if (!stockDeducted)
                    {
                        string itemsQuery = @"
                            SELECT ProductID, Quantity
                            FROM SaleItems WHERE SaleID = @ID";

                        DataTable items = new DataTable();

                        using (SqlDataAdapter adapter =
                            new SqlDataAdapter(itemsQuery, con))
                        {
                            adapter.SelectCommand.Transaction = trans;
                            adapter.SelectCommand.Parameters.AddWithValue("@ID", saleId);
                            adapter.Fill(items);
                        }

                        foreach (DataRow item in items.Rows)
                        {
                            string stockQuery = @"
                                UPDATE Products
                                SET StockQuantity = StockQuantity - @Qty,
                                    Status = CASE
                                        WHEN StockQuantity - @Qty <= 0 THEN 'Out of Stock'
                                        WHEN StockQuantity - @Qty <= ReorderLevel THEN 'Low Stock'
                                        ELSE 'Available' END
                                WHERE ProductID = @ProductID
                                AND IsActive = 1
                                AND StockQuantity >= @Qty";

                            using (SqlCommand cmd = new SqlCommand(stockQuery, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@Qty", item["Quantity"]);
                                cmd.Parameters.AddWithValue("@ProductID", item["ProductID"]);

                                if (cmd.ExecuteNonQuery() == 0)
                                    throw new Exception("Insufficient stock for a product in this sale.");
                            }

                            string logQuery = @"
                                INSERT INTO InventoryLogs
                                (ProductID, [Action], Quantity, Reason)
                                VALUES (@ProductID, 'Sale', @Qty, @Reason)";

                            using (SqlCommand cmd = new SqlCommand(logQuery, con, trans))
                            {
                                cmd.Parameters.AddWithValue("@ProductID", item["ProductID"]);
                                cmd.Parameters.AddWithValue("@Qty", item["Quantity"]);
                                cmd.Parameters.AddWithValue("@Reason", "Sale invoice " + txtInvoiceNumber.Text);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            "UPDATE Sales SET StockDeducted = 1 WHERE SaleID = @ID",
                            con, trans))
                        {
                            cmd.Parameters.AddWithValue("@ID", saleId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    decimal newPaid = previouslyPaid + applied;
                    string status = newPaid >= totalAmount ? "Paid" : "Partially Paid";

                    using (SqlCommand cmd = new SqlCommand(
                        "UPDATE Sales SET PaymentStatus = @Status WHERE SaleID = @ID",
                        con, trans))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@ID", saleId);
                        cmd.ExecuteNonQuery();
                    }

                    trans.Commit();

                    MessageBox.Show(
                        "Payment recorded.\nAmount applied: ₱" + applied.ToString("N2") +
                        "\nChange: ₱" + change.ToString("N2") +
                        "\nRemaining balance: ₱" +
                        Math.Max(0, totalAmount - newPaid).ToString("N2"));

                    LoadSale();
                    txtAmountReceived.Clear();
                    txtChange.Text = "Remaining balance: ₱" + GetBalance().ToString("N2");
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    MessageBox.Show("Payment failed: " + ex.Message);
                }
            }
        }

        private void btnCancelPayment_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnPrintReceipt_Click(object sender, EventArgs e)
        {
            if (saleId <= 0)
            {
                MessageBox.Show("There is no receipt to print.");
                return;
            }

            printPreviewReceipt.ShowDialog();
        }

        private void receiptDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            float y = 40;
            Font heading = new Font("Arial", 16, FontStyle.Bold);
            Font normal = new Font("Arial", 10);

            e.Graphics.DrawString("SALES AND INVENTORY SYSTEM",
                heading, Brushes.Black, 40, y);
            y += 40;

            e.Graphics.DrawString("PAYMENT RECEIPT", heading,
                Brushes.Black, 40, y);
            y += 40;

            e.Graphics.DrawString("Invoice: " + txtInvoiceNumber.Text,
                normal, Brushes.Black, 40, y);
            y += 25;

            e.Graphics.DrawString("Customer: " + txtCustomerName.Text,
                normal, Brushes.Black, 40, y);
            y += 25;

            e.Graphics.DrawString("Total Amount: ₱" + totalAmount.ToString("N2"),
                normal, Brushes.Black, 40, y);
            y += 25;

            e.Graphics.DrawString("Total Paid: ₱" + previouslyPaid.ToString("N2"),
                normal, Brushes.Black, 40, y);
            y += 25;

            e.Graphics.DrawString("Balance: ₱" + GetBalance().ToString("N2"),
                normal, Brushes.Black, 40, y);
            y += 35;

            e.Graphics.DrawString("Thank you for your purchase!",
                normal, Brushes.Black, 40, y);
        }
    }
}
