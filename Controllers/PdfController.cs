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

                var form = await Request.ReadFormAsync();
                var image = form.Files["image"];
                var csvFile = form.Files["tableData"];
                var metadataRaw = form["metadata"];

                if (image == null || csvFile == null || string.IsNullOrWhiteSpace(metadataRaw))
                {
                    return BadRequest("Missing image, CSV file, or metadata.");
                }

                // Time CSV parsing
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

                // Time metadata parsing
                var metadataStopwatch = Stopwatch.StartNew();
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
                metadataStopwatch.Stop();
                Console.WriteLine($"Metadata parsing took {metadataStopwatch.ElapsedMilliseconds} ms");

                using var imageStream = image.OpenReadStream();

                var headerModel = new PdfHeaderModel
                {
                    LogoPath = "Utils/barracuda_logo.png",
                    Title = metadata.Count > 0 ? metadata[0] : "N/A",
                    GeneratedDate = DateTime.Now.ToString("MMM dd, yyyy"),
                    CompanyName = "Barracuda Networks",
                    InspectorName = metadata.Count > 1 ? metadata[1] : "N/A",
                    DateRange = metadata.Count > 2 ? metadata[2] : "N/A"
                };

                var footerModel = new PdfFooterModel
                {
                    ShowPageNumbers = true
                };

                // Time PDF generation
                var pdfStopwatch = Stopwatch.StartNew();
                byte[] pdfBytes = PdfGenerator.Generate(imageStream, tableData, headerModel, footerModel);
                pdfStopwatch.Stop();
                Console.WriteLine($"PDF generation took {pdfStopwatch.ElapsedMilliseconds} ms");

                // Time filename generation
                var fileNameStopwatch = Stopwatch.StartNew();
                string fileName = $"{headerModel.Title}_{headerModel.GeneratedDate}.pdf";
                fileNameStopwatch.Stop();
                Console.WriteLine($"Filename creation took {fileNameStopwatch.ElapsedMilliseconds} ms");

                overallStopwatch.Stop();
                Console.WriteLine($"Total request processing time: {overallStopwatch.ElapsedMilliseconds} ms");

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error generating PDF");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

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
