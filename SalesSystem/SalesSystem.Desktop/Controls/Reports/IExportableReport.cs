namespace SalesSystem.Desktop.Controls.Reports;

public interface IExportableReport
{
    DataGridView GetDataGridView();
    string GetReportName();
}



