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
        private readonly DatabaseConnection database = new DatabaseConnection();
        private string reportType = "Sales";
        private DataTable currentReport = new DataTable();
        private readonly PrintDocument reportDocument = new PrintDocument();
        private int printRowIndex;

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
            reportDocument.BeginPrint += delegate { printRowIndex = 0; };
            reportDocument.PrintPage += reportDocument_PrintPage;
            dgvReportRecords.ReadOnly = true;
            dgvReportRecords.AllowUserToAddRows = false;
            dgvReportRecords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            LoadSalesReport();
        }

        private bool ValidateDates()
        {
            if (dtpStartDate.Value.Date > dtpEndDate.Value.Date)
            {
                MessageBox.Show("Start date cannot be later than end date.");
                return false;
            }
            return true;
        }

        private void btnSalesReport_Click(object sender, EventArgs e)
        {
            reportType = "Sales";
            if (ValidateDates()) LoadSalesReport();
        }

        private void btnInventoryReport_Click(object sender, EventArgs e)
        {
            reportType = "Inventory";
            LoadInventoryReport();
        }

        private void btnGenerateReport_Click(object sender, EventArgs e)
        {
            if (!ValidateDates()) return;
            if (reportType == "Sales") LoadSalesReport();
            else LoadInventoryReport();
        }

        private void LoadSalesReport()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT InvoiceNumber, CustomerName, SaleDate, TotalAmount, PaymentStatus
                    FROM Sales
                    WHERE SaleDate >= @StartDate AND SaleDate < @EndDate
                    ORDER BY SaleDate DESC;", con))
                {
                    adapter.SelectCommand.Parameters.AddWithValue("@StartDate", dtpStartDate.Value.Date);
                    adapter.SelectCommand.Parameters.AddWithValue("@EndDate", dtpEndDate.Value.Date.AddDays(1));
                    currentReport = new DataTable();
                    adapter.Fill(currentReport);
                    dgvReportRecords.DataSource = currentReport;
                }

                using (SqlConnection con = database.GetConnection())
                using (SqlCommand command = new SqlCommand(@"
                    SELECT ISNULL(SUM(TotalAmount), 0)
                    FROM Sales WHERE SaleDate >= @StartDate AND SaleDate < @EndDate;", con))
                {
                    command.Parameters.AddWithValue("@StartDate", dtpStartDate.Value.Date);
                    command.Parameters.AddWithValue("@EndDate", dtpEndDate.Value.Date.AddDays(1));
                    con.Open();
                    lblTotalSalesAmount.Text = "₱" + Convert.ToDecimal(command.ExecuteScalar()).ToString("N2");
                }
                lblTotalProductsCount.Text = GetProductCount().ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Sales report could not be loaded.\n\n" + ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadInventoryReport()
        {
            try
            {
                using (SqlConnection con = database.GetConnection())
                using (SqlDataAdapter adapter = new SqlDataAdapter(@"
                    SELECT ProductID, ProductName, Category, Price, StockQuantity, ReorderLevel, Status
                    FROM Products WHERE IsActive = 1 ORDER BY ProductName;", con))
                {
                    currentReport = new DataTable();
                    adapter.Fill(currentReport);
                    dgvReportRecords.DataSource = currentReport;
                }
                lblTotalProductsCount.Text = GetProductCount().ToString();
                lblTotalSalesAmount.Text = "Not applicable";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Inventory report could not be loaded.\n\n" + ex.Message, "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GetProductCount()
        {
            using (SqlConnection con = database.GetConnection())
            using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Products WHERE IsActive = 1;", con))
            {
                con.Open();
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void btnPrintReport_Click(object sender, EventArgs e)
        {
            if (currentReport == null || currentReport.Rows.Count == 0)
            {
                MessageBox.Show("There are no records to print.");
                return;
            }
            using (PrintPreviewDialog preview = new PrintPreviewDialog())
            {
                preview.Document = reportDocument;
                preview.Width = 1000;
                preview.Height = 700;
                preview.ShowDialog(this);
            }
        }

        private void reportDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            using (Font titleFont = new Font("Arial", 15, FontStyle.Bold))
            using (Font headerFont = new Font("Arial", 8, FontStyle.Bold))
            using (Font rowFont = new Font("Arial", 8))
            {
                float y = e.MarginBounds.Top;
                e.Graphics.DrawString(reportType + " Report", titleFont, Brushes.Black, e.MarginBounds.Left, y);
                y += 35;
                e.Graphics.DrawString("Period: " + dtpStartDate.Value.ToShortDateString() + " to " + dtpEndDate.Value.ToShortDateString(), rowFont, Brushes.Black, e.MarginBounds.Left, y);
                y += 28;

                float columnWidth = (float)e.MarginBounds.Width / Math.Max(1, currentReport.Columns.Count);
                float x = e.MarginBounds.Left;
                foreach (DataColumn column in currentReport.Columns)
                {
                    e.Graphics.DrawString(column.ColumnName, headerFont, Brushes.Black, new RectangleF(x, y, columnWidth - 4, 30));
                    x += columnWidth;
                }
                y += 25;

                while (printRowIndex < currentReport.Rows.Count)
                {
                    if (y > e.MarginBounds.Bottom - 25)
                    {
                        e.HasMorePages = true;
                        return;
                    }
                    DataRow row = currentReport.Rows[printRowIndex];
                    x = e.MarginBounds.Left;
                    foreach (DataColumn column in currentReport.Columns)
                    {
                        string value = Convert.ToString(row[column]);
                        if (value.Length > 25) value = value.Substring(0, 22) + "...";
                        e.Graphics.DrawString(value, rowFont, Brushes.Black, new RectangleF(x, y, columnWidth - 4, 25));
                        x += columnWidth;
                    }
                    y += 20;
                    printRowIndex++;
                }
                e.HasMorePages = false;
            }
        }

        private void btnCloseReport_Click(object sender, EventArgs e) { Close(); }
        private void panel3_Paint(object sender, PaintEventArgs e) { }

        private void Reports_Form_Load(object sender, EventArgs e)
        {

        }
    }
}
