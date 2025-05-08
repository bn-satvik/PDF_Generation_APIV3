
# 📄 PDF Generation API (.NET 8)

This API generates a polished multi-page PDF from a CSV file, an image, and some metadata. The image appears on the first page, while the CSV content is rendered as a clean, styled table across the following pages.

---

## 🔧 Technologies Used

- ASP.NET Core Web API (.NET 8)
- MigraDoc & PdfSharp for PDF layout and rendering
- CsvHelper for robust CSV parsing
- SixLabors.ImageSharp for image processing

---

## 📦 NuGet Packages

```xml
<PackageReference Include="CsvHelper" Version="33.0.1" />
<PackageReference Include="PdfSharp" Version="6.1.1" />
<PackageReference Include="PdfSharp-MigraDoc" Version="6.1.1" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.8" />
````

---

## 🚀 API Endpoint

**POST** `/api/pdf/generate`

Generates a downloadable PDF with image and tabular content.

### 📥 Request (multipart/form-data)

| Key         | Type        | Description                                  |
| ----------- | ----------- | -------------------------------------------- |
| `image`     | File        | Image file to be shown on the first page     |
| `tableData` | File (.csv) | CSV file with data to be rendered as a table |
| `metadata`  | Text (JSON) | JSON array: `[Title, Product, Date Range]`   |

**Example metadata:**

```json
["Feeds", "Data Inspector", "01-02-2024 to 02-02-2024"]
```

---

## 📤 Output

Returns a `.pdf` file with:

* **Page 1:** The uploaded image
* **Page 2+:** A clean, auto-fitted table generated from the CSV content

---

## 📂 Sample CSV Format

```csv
Row,Recipient Email,Received Date,Clicked on Links,Sender Email,Subject
1,email@example.com,2024-04-01,Yes,sender@example.com,Sample Subject
```

---

## 🛠 Running the Project

1. Clone the repo:

```bash
git clone https://github.com/bn-satvik/PDF_Generation_APIV3.git
cd PDF-Generation-API-4
```

2. Restore dependencies:

```bash
dotnet restore
```

3. Run the API:

```bash
dotnet run
```

---

## 📌 Example Request (cURL)

```bash
curl -X POST https://yourapiurl/api/pdf/generate \
  -F "image=@/path/to/image.jpg" \
  -F "tableData=@/path/to/data.csv" \
  -F "metadata=[\"Feeds\", \"Data Inspector\", \"01-02-2024 to 02-02-2024\"]"
```

---

## 📝 Notes

* CSV must include a header row and at least one data row.
* Font sizes and column widths are optimized for print and screen readability.
* Headers and footers are consistently applied to each page using metadata.

---

## ✅ Conclusion

This project provides a simple and efficient solution to convert structured data and images into well-formatted PDFs using a clean .NET 8 Web API.

```

Let me know if you’d like this converted into a downloadable file or adjusted for deployment environments (e.g., Docker or Azure).
```
