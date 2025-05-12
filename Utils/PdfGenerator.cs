using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using SixLabors.ImageSharp;
using System.Text;
using System.Diagnostics;
namespace Proj.Utils
{
    public static class PdfGenerator
    {
        private const double Dpi = 96;
        private const double TargetImageWidthCm = 15.0;
        private const double MarginCm = 1.0;
        private const double MinImagePageWidthCm = 16.0;
        private const double MinImagePageHeightCm = 16.0;
        private const double CharWidthCm = 0.23;
        private const double MinColWidthCm = 2.0;
        private const double MaxColWidthCm = 10.0;
        private const double MaxPageWidthCm = 70.0;
        private const double DefaultPageWidthCm = 21.0;
        private const double PageHeightCm = 34;
        private const int DefaultFontSize = 10;
        private const int HeaderFontSize = 12;
        private const int ColumnFontSize = 9;
        private const string HeaderSpaceBefore = "0.3cm";
        private const string HeaderSpaceAfter = "0.3cm";
        private const string DataSpaceBefore = "0.15cm";
        private const string DataSpaceAfter = "0.15cm";
        private const double CellBorderWidth = 0.5;
        private const int SoftBreakInterval = 20;
        public static byte[] Generate(Stream imageStream, List<List<string>> tableData, PdfHeaderModel headerModel, PdfFooterModel footerModel)
        {
            var stopwatch = Stopwatch.StartNew();
            Console.WriteLine("PDF generation started.");

            if (tableData == null || tableData.Count < 2)
                throw new ArgumentException("Table data must include at least one header row and one data row.");

            var headerRow = tableData[0];
            var dataRows = tableData.Skip(1).ToList();
            var document = new Document();

            // === IMAGE SECTION ===
            var imageSectionWatch = Stopwatch.StartNew();
            var imageSection = document.AddSection();
            var imageBytes = ReadFully(imageStream);
            string base64Image = Convert.ToBase64String(imageBytes);
            string imageUri = "base64:" + base64Image;

            using (var ms = new MemoryStream(imageBytes))
            {
                var imgInfo = Image.Identify(ms);
                if (imgInfo == null)
                    throw new InvalidOperationException("Could not identify image.");

                double originalWidthCm = imgInfo.Width / Dpi * 2.54;
                double originalHeightCm = imgInfo.Height / Dpi * 2.54;
                double aspectRatio = originalHeightCm / originalWidthCm;
                double newHeightCm = TargetImageWidthCm * aspectRatio;
                double headerFooterPadding = 6.0;

                double pageWidth = Math.Max(MinImagePageWidthCm, TargetImageWidthCm + 2 * MarginCm);
                double pageHeight = Math.Max(MinImagePageHeightCm, newHeightCm + 2 * MarginCm + headerFooterPadding);

                imageSection.PageSetup.PageWidth = Unit.FromCentimeter(pageWidth);
                imageSection.PageSetup.PageHeight = Unit.FromCentimeter(pageHeight);
                imageSection.PageSetup.LeftMargin = Unit.FromCentimeter(MarginCm);
                imageSection.PageSetup.RightMargin = Unit.FromCentimeter(MarginCm);
                imageSection.PageSetup.TopMargin = Unit.FromCentimeter(MarginCm);
                imageSection.PageSetup.BottomMargin = Unit.FromCentimeter(MarginCm);
            }

            PdfHeaderLayout.BuildHeader(imageSection, headerModel);
            PdfFooterLayout.BuildFooter(footerModel, imageSection);

            var imageParagraph = imageSection.AddParagraph();
            imageParagraph.Format.SpaceBefore = Unit.FromCentimeter(1);
            imageParagraph.Format.Alignment = ParagraphAlignment.Center;
            var image = imageParagraph.AddImage(imageUri);
            image.Width = $"{TargetImageWidthCm}cm";
            image.LockAspectRatio = true;
            imageSectionWatch.Stop();
            Console.WriteLine($"Image section built in {imageSectionWatch.ElapsedMilliseconds} ms");

            // === TABLE SECTION ===
            var tableSectionWatch = Stopwatch.StartNew();
            var tableSection = document.AddSection();
            int columnCount = headerRow.Count;
            var columnWidths = new double[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                columnWidths[i] = CalculateColumnWidth(headerRow, dataRows, i, CharWidthCm, MinColWidthCm, MaxColWidthCm);
            }

            double tableWidth = columnWidths.Sum();
            double pageWidthTable = Math.Max(Math.Min(tableWidth + 3.0, MaxPageWidthCm), DefaultPageWidthCm);
            double margin = (pageWidthTable - tableWidth) / 2;

            tableSection.PageSetup.PageWidth = Unit.FromCentimeter(pageWidthTable);
            tableSection.PageSetup.LeftMargin = Unit.FromCentimeter(margin);
            tableSection.PageSetup.RightMargin = Unit.FromCentimeter(margin);
            tableSection.PageSetup.PageHeight = Unit.FromCentimeter(PageHeightCm);
            tableSection.PageSetup.TopMargin = Unit.FromCentimeter(2);
            tableSection.PageSetup.BottomMargin = Unit.FromCentimeter(2.5);

            PdfHeaderLayout.BuildHeader(tableSection, headerModel);
            PdfFooterLayout.BuildFooter(footerModel, tableSection);

            var table = tableSection.AddTable();
            table.Borders.Width = 0;
            table.Format.Font.Size = DefaultFontSize;
            table.KeepTogether = false;

            for (int i = 0; i < columnCount; i++)
            {
                var column = table.AddColumn(Unit.FromCentimeter(columnWidths[i]));
                column.Format.Font.Size = ColumnFontSize;
            }

            var headerRowObj = table.AddRow();
            headerRowObj.HeadingFormat = true;
            headerRowObj.Format.Font.Bold = true;

            for (int i = 0; i < columnCount; i++)
            {
                var cell = headerRowObj.Cells[i];
                var para = cell.AddParagraph(InsertSoftBreaks(headerRow[i], SoftBreakInterval));
                para.Format.Alignment = ParagraphAlignment.Left;
                para.Format.Font.Size = HeaderFontSize;
                cell.VerticalAlignment = VerticalAlignment.Center;
                para.Format.SpaceBefore = HeaderSpaceBefore;
                para.Format.SpaceAfter = HeaderSpaceAfter;
                cell.Borders.Bottom.Width = CellBorderWidth;
            }

            foreach (var row in dataRows)
            {
                var dataRow = table.AddRow();
                for (int j = 0; j < columnCount; j++)
                {
                    var cell = dataRow.Cells[j];
                    var text = j < row.Count ? InsertSoftBreaks(row[j], SoftBreakInterval) : "";
                    var para = cell.AddParagraph(text);
                    para.Format.Alignment = ParagraphAlignment.Left;
                    para.Format.Font.Size = DefaultFontSize;
                    cell.VerticalAlignment = VerticalAlignment.Center;
                    para.Format.SpaceBefore = DataSpaceBefore;
                    para.Format.SpaceAfter = DataSpaceAfter;
                    cell.Borders.Bottom.Width = CellBorderWidth;
                    cell.Borders.Bottom.Color = Colors.Gray;
                }
            }
            tableSectionWatch.Stop();
            Console.WriteLine($"Table section built in {tableSectionWatch.ElapsedMilliseconds} ms");

            // === RENDER PDF ===
            var renderWatch = Stopwatch.StartNew();
            var pdfRenderer = new PdfDocumentRenderer { Document = document };
            pdfRenderer.RenderDocument();
            renderWatch.Stop();
            Console.WriteLine($"Document rendered in {renderWatch.ElapsedMilliseconds} ms");

            // === SAVE TO STREAM ===
            var saveWatch = Stopwatch.StartNew();
            using var finalMs = new MemoryStream();
            pdfRenderer.PdfDocument.Save(finalMs, false);
            saveWatch.Stop();
            Console.WriteLine($"PDF saved in {saveWatch.ElapsedMilliseconds} ms");

            stopwatch.Stop();
            Console.WriteLine($"Total PDF generation time: {stopwatch.ElapsedMilliseconds} ms");

            return finalMs.ToArray();
        }


        private static double CalculateColumnWidth(List<string> headerRow, List<List<string>> dataRows, int columnIndex, double charWidthCm, double minCm, double maxCm)
        {
            var header = headerRow[columnIndex] ?? "";
            int maxWordLen = header.Split(' ', StringSplitOptions.RemoveEmptyEntries).DefaultIfEmpty("").Max(w => w.Length);
            var lengths = new List<int> { header.Length };

            foreach (var row in dataRows)
            {
                if (columnIndex < row.Count && row[columnIndex] != null)
                    lengths.Add(row[columnIndex].Length);
            }

            int maxLen = lengths.Max();
            int referenceLen = 10;
            int maxValue = Math.Max(maxWordLen, Math.Max(maxLen, referenceLen));
            return Math.Min(Math.Max(maxValue * charWidthCm, minCm), maxCm);
        }

        private static byte[] ReadFully(Stream input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        private static string InsertSoftBreaks(string input, int interval)
        {
            if (string.IsNullOrEmpty(input) || input.Length < interval)
                return input;

            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                if (i > 0 && i % interval == 0)
                    sb.Append('\u200B');
                sb.Append(input[i]);
            }
            return sb.ToString();
        }
    }
}
