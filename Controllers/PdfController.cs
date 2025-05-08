using Microsoft.AspNetCore.Mvc;
using Proj.Utils;
using System.Text.Json;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

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

        // API endpoint: POST /api/pdf/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePdf()
        {
            try
            {
                // Read form data from request
                var form = await Request.ReadFormAsync();

                // Get image file, CSV file, and metadata from form
                var image = form.Files["image"];
                var csvFile = form.Files["tableData"];
                var metadataRaw = form["metadata"]; // JSON string: [Title, Inspector, Date Range]

                // Check if any required input is missing
                if (image == null || csvFile == null || string.IsNullOrWhiteSpace(metadataRaw))
                {
                    return BadRequest("Missing image, CSV file, or metadata.");
                }

                // Parse CSV into table data (list of rows) using CsvHelper
                List<List<string>> tableData = new List<List<string>>();
                using (var csvStream = csvFile.OpenReadStream())
                {
                    tableData = ParseCsvWithCsvHelper(csvStream); // Use CsvHelper
                }

                // Ensure at least header + one data row
                if (tableData.Count < 2)
                {
                    return BadRequest("CSV must contain a header row and at least one data row.");
                }

                // Parse metadata JSON into list of strings safely
                List<string> metadata;
                try
                {
                    var deserialized = JsonSerializer.Deserialize<List<string>>(metadataRaw.ToString());
                    if (deserialized is null)
                    {
                        return BadRequest("Failed to parse metadata.");
                    }
                    metadata = deserialized;
                }
                catch (JsonException ex)
                {
                    return BadRequest($"Invalid JSON format for metadata: {ex.Message}");
                }


                // Get image stream for PDF
                using var imageStream = image.OpenReadStream();

                // Prepare PDF header data
                var headerModel = new PdfHeaderModel
                {
                    LogoPath = "Utils/barracuda_logo.png",
                    Title = metadata.Count > 0 ? metadata[0] : "N/A",
                    GeneratedDate = DateTime.Now.ToString("MMM dd, yyyy"),
                    CompanyName = "Barracuda Networks",
                    InspectorName = metadata.Count > 1 ? metadata[1] : "N/A",
                    DateRange = metadata.Count > 2 ? metadata[2] : "N/A"
                };

                // Set footer options
                var footerModel = new PdfFooterModel
                {
                    ShowPageNumbers = true
                };

                // Generate the PDF using helper class
                byte[] pdfBytes = PdfGenerator.Generate(imageStream, tableData, headerModel, footerModel);

                // Create filename using metadata title and date
                string fileName = $"{headerModel.Title}_{headerModel.GeneratedDate}.pdf";

                // Return PDF file as HTTP response
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                _logger.LogError(ex, "Error generating PDF");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Parses CSV with CsvHelper and returns a list of rows
        private List<List<string>> ParseCsvWithCsvHelper(Stream csvStream)
        {
            var records = new List<List<string>>();

            // Set up the CsvReader to handle CSV data
            using (var reader = new StreamReader(csvStream, Encoding.UTF8))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ","
            }))
            {
                // Read records and store each row as a list of strings
                while (csv.Read())
                {
                    var row = new List<string>();

                    // Iterate through each column in the row
                    for (int i = 0; csv.TryGetField(i, out string? value); i++)
                    {
                        row.Add(value ?? string.Empty); // Safe null fallback
                    }

                    records.Add(row);
                }
            }

            return records;
        }
    }
}
