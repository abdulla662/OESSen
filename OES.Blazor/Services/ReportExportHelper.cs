using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text;

namespace OES.Blazor.Services
{
    internal sealed class ExportSheet
    {
        public string Name { get; }
        public string[] Headers { get; }
        public IEnumerable<IEnumerable<object?>> Rows { get; }

        public ExportSheet(string name, string[] headers, IEnumerable<IEnumerable<object?>> rows)
        {
            Name = name;
            Headers = headers;
            Rows = rows;
        }
    }

    internal static class ReportExportHelper
    {
        private const int CsvRowThreshold = 100_000;
        private static readonly Color HeaderBlue = Color.FromArgb(0x15, 0x65, 0xC0);

        public static (byte[] Bytes, string Extension) Build(string title, params ExportSheet[] sheets)
        {
            int totalRows = sheets.Sum(s => s.Rows.Count());

            return totalRows > CsvRowThreshold
                ? (BuildCsv(title, sheets), "csv")
                : (BuildExcel(sheets), "xlsx");
        }

        private static byte[] BuildExcel(ExportSheet[] sheets)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var pkg = new ExcelPackage();

            bool isRtl = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

            foreach (var sheet in sheets)
            {
                var ws = pkg.Workbook.Worksheets.Add(sheet.Name);
                ws.View.RightToLeft = isRtl;
                ws.Cells.Style.Font.Name = "Arial";

                for (int c = 0; c < sheet.Headers.Length; c++)
                {
                    var cell = ws.Cells[1, c + 1];
                    cell.Value = sheet.Headers[c];
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.Color.SetColor(Color.White);
                    cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    cell.Style.Fill.BackgroundColor.SetColor(HeaderBlue);
                    cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var dataRow in sheet.Rows)
                {
                    int col = 1;
                    foreach (var val in dataRow)
                    {
                        var cell = ws.Cells[row, col++];
                        cell.Value = val;
                        cell.Style.Font.Name = "Arial";

                        cell.Style.HorizontalAlignment = val switch
                        {
                            int or double or float or decimal or long => ExcelHorizontalAlignment.Center,
                            _ => isRtl ? ExcelHorizontalAlignment.Right : ExcelHorizontalAlignment.Left
                        };
                    }
                    row++;
                }

                if (ws.Dimension != null)
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
            }

            return pkg.GetAsByteArray();
        }

        private static byte[] BuildCsv(string title, ExportSheet[] sheets)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Esc(title));
            sb.AppendLine();

            foreach (var sheet in sheets)
            {
                sb.AppendLine(Esc(sheet.Name));
                sb.AppendLine(string.Join(",", sheet.Headers.Select(Esc)));
                foreach (var row in sheet.Rows)
                    sb.AppendLine(string.Join(",", row.Select(v => Esc(v?.ToString()))));
                sb.AppendLine();
            }

            // UTF-8 BOM so Arabic renders correctly when opened in Excel
            var bom = Encoding.UTF8.GetPreamble();
            var body = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[bom.Length + body.Length];
            bom.CopyTo(result, 0);
            body.CopyTo(result, bom.Length);
            return result;
        }

        private static string Esc(string? v)
        {
            if (string.IsNullOrEmpty(v)) return string.Empty;
            return (v.Contains(',') || v.Contains('"') || v.Contains('\n'))
                ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
        }
    }
}
