
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Sales___Inventory_Log_In
{
    public partial class Reports_Form : Form
    {
        private DatabaseConnection database = new DatabaseConnection();
        private string reportType = "Sales";
        private DataTable currentReport = new DataTable();
        private PrintDocument reportDocument = new PrintDocument();

        public Reports_Form()
        {
            InitializeComponent();

            dtpStartDate.Value = DateTime.Today.AddDays(-30);
            dtpEndDate.Value = DateTime.Today;

            btnSalesReport.Click += btnSalesReport_Click;
            btnInventoryReport.Click += btnInventoryReport_Click;
            btnGenerateReport.Click += btnGenerateReport_Click;
            btnPrintReport.Click += btnPrintReport_Click;
            btnCloseReport.Click += btnCloseReport_Click;

            reportDocument.PrintPage += reportDocument_PrintPage;

            dgvReportRecords.ReadOnly = true;
            dgvReportRecords.AllowUserToAddRows = false;
            dgvReportRecords.AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill;

            LoadSalesReport();
        }

        private void btnSalesReport_Click(object sender, EventArgs e)
        {
            reportType = "Sales";
            LoadSalesReport();
        }

        private void btnInventoryReport_Click(object sender, EventArgs e)
        {
            reportType = "Inventory";
            LoadInventoryReport();
        }

        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
            if (dtpStartDate.Value.Date > dtpEndDate.Value.Date)
            {
                MessageBox.Show("Start date cannot be later than end date.");
                return;
            }

            if (reportType == "Sales")
                LoadSalesReport();
            else
                LoadInventoryReport();
        }

        private void LoadSalesReport()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    string query = @"
                        SELECT InvoiceNumber, CustomerName, SaleDate,
                               TotalAmount, PaymentStatus
                        FROM Sales
                        WHERE SaleDate >= @StartDate
                        AND SaleDate < @EndDate
                        ORDER BY SaleDate DESC";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);
                    adapter.SelectCommand.Parameters.AddWithValue(
                        "@StartDate", dtpStartDate.Value.Date);
                    adapter.SelectCommand.Parameters.AddWithValue(
                        "@EndDate", dtpEndDate.Value.Date.AddDays(1));

                    currentReport = new DataTable();
                    adapter.Fill(currentReport);
                    dgvReportRecords.DataSource = currentReport;

                    string totalQuery = @"
                        SELECT ISNULL(SUM(TotalAmount), 0)
                        FROM Sales
                        WHERE SaleDate >= @StartDate
                        AND SaleDate < @EndDate";

                    using (SqlCommand cmd = new SqlCommand(totalQuery, con))
                    {
                        cmd.Parameters.AddWithValue(
                            "@StartDate", dtpStartDate.Value.Date);
                        cmd.Parameters.AddWithValue(
                            "@EndDate", dtpEndDate.Value.Date.AddDays(1));

                        con.Open();
                        lblTotalSalesAmount.Text =
                            "₱" + Convert.ToDecimal(cmd.ExecuteScalar()).ToString("N2");
                    }

                    lblTotalProductsCount.Text = GetProductCount().ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sales report error: " + ex.Message);
            }
        }

        private void LoadInventoryReport()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                {
                    string query = @"
                        SELECT ProductID, ProductName, Category, Price,
                               StockQuantity, ReorderLevel, Status
                        FROM Products
                        WHERE IsActive = 1
                        ORDER BY ProductName";

                    SqlDataAdapter adapter = new SqlDataAdapter(query, con);

                    currentReport = new DataTable();
                    adapter.Fill(currentReport);
                    dgvReportRecords.DataSource = currentReport;

                    lblTotalProductsCount.Text = GetProductCount().ToString();

                    lblTotalSalesAmount.Text = "₱0.00";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Inventory report error: " + ex.Message);
            }
        }

        private int GetProductCount()
        {
            using (SqlConnection con = database.GetConnection())
            {
                SqlCommand cmd = new SqlCommand(
                    "SELECT COUNT(*) FROM Products WHERE IsActive = 1", con);

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private void btnPrintReport_Click(object sender, EventArgs e)
        {
            if (currentReport == null || currentReport.Rows.Count == 0)
            {
                MessageBox.Show("There are no records to print.");
                return;
            }

            PrintPreviewDialog preview = new PrintPreviewDialog();
            preview.Document = reportDocument;
            preview.Width = 900;
            preview.Height = 650;
            preview.ShowDialog();
        }

        private void reportDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            float y = 35;
            Font titleFont = new Font("Arial", 15, FontStyle.Bold);
            Font headerFont = new Font("Arial", 9, FontStyle.Bold);
            Font rowFont = new Font("Arial", 8);

            e.Graphics.DrawString(reportType + " Report",
                titleFont, Brushes.Black, 35, y);
            y += 35;

            e.Graphics.DrawString(
                "Period: " + dtpStartDate.Value.ToShortDateString() +
                " to " + dtpEndDate.Value.ToShortDateString(),
                rowFont, Brushes.Black, 35, y);
            y += 30;

            float x = 35;
            float columnWidth = 780f / Math.Max(1, currentReport.Columns.Count);

            foreach (DataColumn column in currentReport.Columns)
            {
                e.Graphics.DrawString(column.ColumnName,
                    headerFont, Brushes.Black, x, y);
                x += columnWidth;
            }

            y += 25;

            foreach (DataRow row in currentReport.Rows)
            {
                x = 35;

                foreach (DataColumn column in currentReport.Columns)
                {
                    string value = Convert.ToString(row[column]);
                    if (value.Length > 18)
                        value = value.Substring(0, 18);

                    e.Graphics.DrawString(value, rowFont,
                        Brushes.Black, x, y);
                    x += columnWidth;
                }

                y += 20;

                if (y > e.MarginBounds.Bottom - 30)
                {
                    e.HasMorePages = true;
                    return;
                }
            }

            e.HasMorePages = false;
        }

        private void btnCloseReport_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void panel3_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
