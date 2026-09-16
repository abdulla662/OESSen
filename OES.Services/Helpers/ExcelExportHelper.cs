using ClosedXML.Excel;

namespace OES.Services.Helpers
{
    public static class ExcelExportHelper
    {
        public class ColumnDefinition<T>
        {
            public string Header { get; set; }
            public Func<T, object> ValueSelector { get; set; }
        }

        private const int MaxRowsPerSheet = 1_048_575; // Excel limit minus the header row

        // Selected columns
        public static byte[] GenerateExcelBytes<T>(IEnumerable<T> data, string sheetName, List<ColumnDefinition<T>> columns)
        {
            using var workbook = new XLWorkbook();

            var dataList = data as IList<T> ?? [.. data];
            int sheetNumber = 1;
            int dataIndex = 0;

            do
            {
                var wsName = sheetNumber == 1 ? sheetName : $"{sheetName} ({sheetNumber})";
                var ws = workbook.Worksheets.Add(wsName);

                // 1. Generate Headers
                for (int i = 0; i < columns.Count; i++)
                {
                    var cell = ws.Cell(1, i + 1);
                    cell.Value = columns[i].Header;
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                }

                // 2. Generate Rows for current sheet
                int rowIndex = 2;
                int rowsWritten = 0;
                while (dataIndex < dataList.Count && rowsWritten < MaxRowsPerSheet)
                {
                    for (int colIndex = 0; colIndex < columns.Count; colIndex++)
                        ws.Cell(rowIndex, colIndex + 1).Value = columns[colIndex].ValueSelector(dataList[dataIndex])?.ToString();

                    rowIndex++;
                    dataIndex++;
                    rowsWritten++;
                }

                ws.Columns().AdjustToContents();
                sheetNumber++;
            }
            while (dataIndex < dataList.Count);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public static string GenerateExcelBase64<T>(IEnumerable<T> data, string sheetName, List<ColumnDefinition<T>> columns)
            => Convert.ToBase64String(GenerateExcelBytes(data, sheetName, columns));

        // All columns
        public static string GenerateExcelFromProperties<T>(IEnumerable<T> data, string sheetName)
        {
            var properties = typeof(T).GetProperties();

            var columns = properties.Select(p => new ColumnDefinition<T>
            {
                Header = p.Name,
                ValueSelector = x => p.GetValue(x)
            }).ToList();

            return GenerateExcelBase64(data, sheetName, columns);
        }
    }
}
