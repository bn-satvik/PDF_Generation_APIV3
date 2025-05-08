using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Proj.Utils
{
    public static class PdfGenerator
    {
        public static byte[] Generate(Stream imageStream, List<List<string>> tableData, PdfHeaderModel headerModel, PdfFooterModel footerModel)
        {
            if (tableData == null || tableData.Count < 2)
                throw new ArgumentException("Table data must include at least one header row and one data row.");

            var headerRow = tableData[0];
            var dataRows = tableData.Skip(1).ToList();
            var document = new Document();

            // ===== IMAGE PAGE =====
            var imageSection = document.AddSection();
            var imageBytes = ReadFully(imageStream);
            string base64Image = Convert.ToBase64String(imageBytes);
            string imageUri = "base64:" + base64Image;

            using (var imgStream = new MemoryStream(imageBytes))
            {
                var imgInfo = Image.Identify(imgStream);
                if (imgInfo == null)
                    throw new InvalidOperationException("Could not identify image.");

                const double dpi = 96;
                double widthCm = Math.Max(imgInfo.Width / dpi * 2.54, 15.0);
                double heightCm = Math.Max(imgInfo.Height / dpi * 2.54 + 5, 20.0);

                imageSection.PageSetup.PageWidth = Unit.FromCentimeter(widthCm);
                imageSection.PageSetup.PageHeight = Unit.FromCentimeter(heightCm);
            }

            PdfHeaderLayout.BuildHeader(imageSection, headerModel);
            PdfFooterLayout.BuildFooter(footerModel, imageSection);

            var imageParagraph = imageSection.AddParagraph();
            imageParagraph.Format.SpaceBefore = "2cm";
            imageParagraph.Format.Alignment = ParagraphAlignment.Center;
            var image = imageParagraph.AddImage(imageUri);
            image.Width = "18cm";
            image.LockAspectRatio = true;

            // ===== TABLE SECTION =====
            var tableSection = document.AddSection();
            int columnCount = headerRow.Count;

            // Estimate column widths
            double charWidthCm = 0.23;
            double minColWidthCm = 2;
            double maxColWidthCm = 10;
            var columnWidthsCm = new double[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                columnWidthsCm[i] = CalculateColumnWidth(headerRow, dataRows, i, charWidthCm, minColWidthCm, maxColWidthCm);
            }

            double tableWidthCm = columnWidthsCm.Sum();
            double totalPageWidth = Math.Max(Math.Min(tableWidthCm + 3.0, 70.0), 21.0);
            double margin = (totalPageWidth - tableWidthCm) / 2;

            tableSection.PageSetup.PageWidth = Unit.FromCentimeter(totalPageWidth);
            tableSection.PageSetup.LeftMargin = Unit.FromCentimeter(margin);
            tableSection.PageSetup.RightMargin = Unit.FromCentimeter(margin);
            tableSection.PageSetup.PageHeight = Unit.FromCentimeter(29.7);

            PdfHeaderLayout.BuildHeader(tableSection, headerModel);
            PdfFooterLayout.BuildFooter(footerModel, tableSection);

            var table = tableSection.AddTable();
            table.Borders.Width = 0;
            table.Format.Font.Size = 10;
            table.KeepTogether = false;

            for (int i = 0; i < columnCount; i++)
            {
                var col = table.AddColumn(Unit.FromCentimeter(columnWidthsCm[i]));
                col.Format.Font.Size = 9;
            }

            var headerRowObj = table.AddRow();
            headerRowObj.HeadingFormat = true;
            headerRowObj.Format.Font.Bold = true;
            headerRowObj.Format.Alignment = ParagraphAlignment.Left;

            for (int i = 0; i < columnCount; i++)
            {
                var cell = headerRowObj.Cells[i];
                var para = cell.AddParagraph(InsertSoftBreaks(headerRow[i]));
                para.Format.Alignment = ParagraphAlignment.Left;
                para.Format.Font.Size = 12;
                cell.VerticalAlignment = VerticalAlignment.Center;
                para.Format.SpaceBefore = "0.3cm";
                para.Format.SpaceAfter = "0.3cm";
                cell.Borders.Bottom.Width = 0.5;
            }

            foreach (var row in dataRows)
            {
                var dataRow = table.AddRow();
                for (int j = 0; j < columnCount; j++)
                {
                    var cell = dataRow.Cells[j];
                    var text = j < row.Count ? InsertSoftBreaks(row[j]) : "";
                    var para = cell.AddParagraph(text);
                    para.Format.Alignment = ParagraphAlignment.Left;
                    para.Format.Font.Size = 10;
                    cell.VerticalAlignment = VerticalAlignment.Center;
                    para.Format.SpaceBefore = "0.15cm";
                    para.Format.SpaceAfter = "0.15cm";
                    cell.Borders.Bottom.Width = 0.5;
                    cell.Borders.Bottom.Color = Colors.Gray;
                }
            }

            var pdfRenderer = new PdfDocumentRenderer()
            {
                Document = document
            };
            pdfRenderer.RenderDocument();

            using var ms = new MemoryStream();
            pdfRenderer.PdfDocument.Save(ms, false);
            return ms.ToArray();
        }

        private static double CalculateColumnWidth(List<string> headerRow, List<List<string>> dataRows, int columnIndex, double charWidthCm, double minCm, double maxCm)
        {
            var header = headerRow[columnIndex];
            int maxWordLengthInHeader = header?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Max(w => w.Length) ?? 0;

            var lengths = new List<int> { header?.Length ?? 0 };
            foreach (var row in dataRows)
            {
                if (columnIndex < row.Count && row[columnIndex] != null)
                    lengths.Add(row[columnIndex].Length);
            }

            int maxLen = lengths.Max();
            int referenceValue = 10;
            int maxOfThree = Math.Max(maxWordLengthInHeader, Math.Max(maxLen, referenceValue));

            double columnWidth = maxOfThree * charWidthCm;
            return Math.Min(Math.Max(columnWidth, minCm), maxCm);
        }

        private static byte[] ReadFully(Stream input)
        {
            using var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        private static string InsertSoftBreaks(string input, int interval = 20)
        {
            if (string.IsNullOrEmpty(input) || input.Length < interval)
                return input;

            return string.Concat(input.Select((c, i) => (i > 0 && i % interval == 0) ? "\u200B" + c : c.ToString()));
        }
    }
}
