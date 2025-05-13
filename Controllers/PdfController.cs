using Microsoft.AspNetCore.Mvc;
using Proj.Utils;
using System.Text.Json;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Diagnostics;

namespace Proj.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly ILogger<PdfController> _logger;

        public PdfController(ILogger<PdfController> logger)
        {
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePdf()
        {
            try
            {
                Console.WriteLine("************************************************************");
                var overallStopwatch = Stopwatch.StartNew();

                // Read uploaded form data
                var form = await Request.ReadFormAsync();
                var image = form.Files["image"];
                var csvFile = form.Files["tableData"];
                var metadataRaw = form["metadata"];

                // Validate inputs
                if (image == null || csvFile == null || string.IsNullOrWhiteSpace(metadataRaw))
                {
                    return BadRequest("Missing image, CSV file, or metadata.");
                }

                // Parse CSV data
                var csvStopwatch = Stopwatch.StartNew();
                List<List<string>> tableData;
                using (var csvStream = csvFile.OpenReadStream())
                {
                    tableData = ParseCsvWithCsvHelper(csvStream);
                }
                csvStopwatch.Stop();
                Console.WriteLine($"CSV parsing took {csvStopwatch.ElapsedMilliseconds} ms");

                if (tableData.Count < 2)
                {
                    return BadRequest("CSV must contain a header row and at least one data row.");
                }

                // Parse metadata (Dictionary<string, string>)
                var metadataStopwatch = Stopwatch.StartNew();
                Dictionary<string, string> metadata;
                try
                {
                    metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataRaw.ToString()!)!;
                    if (metadata is null)
                    {
                        return BadRequest("Failed to parse metadata.");
                    }
                }
                catch (JsonException ex)
                {
                    return BadRequest($"Invalid JSON format for metadata: {ex.Message}");
                }
                metadataStopwatch.Stop();
                Console.WriteLine($"Metadata parsing took {metadataStopwatch.ElapsedMilliseconds} ms");

                using var imageStream = image.OpenReadStream();

                // Prepare header data for PDF
                var headerModel = new PdfHeaderModel
                {
                    LogoPath = "Utils/barracuda_logo.png",
                    Title = metadata.TryGetValue("Title", out var title) ? title : "N/A",
                    GeneratedDate = DateTime.Now.ToString("MMM dd, yyyy"),
                    CompanyName = "Barracuda Networks",
                    ProuductName = metadata.TryGetValue("ProductName", out var productName) ? productName : "N/A",
                    DateRange = metadata.TryGetValue("DateRange", out var dateRange) ? dateRange : "N/A"
                };

                // Footer settings
                var footerModel = new PdfFooterModel
                {
                    ShowPageNumbers = true
                };

                // Generate PDF
                var pdfStopwatch = Stopwatch.StartNew();
                byte[] pdfBytes = PdfGenerator.Generate(imageStream, tableData, headerModel, footerModel);
                pdfStopwatch.Stop();
                Console.WriteLine($"PDF generation took {pdfStopwatch.ElapsedMilliseconds} ms");

                // Create filename
                var fileNameStopwatch = Stopwatch.StartNew();
                string fileName = $"{headerModel.Title}_{headerModel.GeneratedDate}.pdf";
                fileNameStopwatch.Stop();
                Console.WriteLine($"Filename creation took {fileNameStopwatch.ElapsedMilliseconds} ms");

                // Log total time
                overallStopwatch.Stop();
                Console.WriteLine($"Total request processing time: {overallStopwatch.ElapsedMilliseconds} ms");

                // Return PDF file
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating PDF");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Helper method to parse CSV
        private List<List<string>> ParseCsvWithCsvHelper(Stream csvStream)
        {
            var records = new List<List<string>>();
            using (var reader = new StreamReader(csvStream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            }))
            {
                while (csv.Read())
                {
                    var row = new List<string>();
                    for (int i = 0; csv.TryGetField(i, out string? value); i++)
                    {
                        row.Add(value ?? string.Empty);
                    }
                    records.Add(row);
                }
            }
            return records;
        }
    }
}
