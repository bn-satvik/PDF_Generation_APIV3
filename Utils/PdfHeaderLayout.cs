using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;


namespace Proj.Utils
{
    public static class PdfHeaderLayout
    {
        // Builds both left and right header frames
        public static void BuildHeader(Section section, PdfHeaderModel headerModel)
        {
            section.PageSetup.TopMargin = "5cm"; // Reserve space for header
            CreateLeftHeader(section, headerModel);
            CreateRightHeader(section, headerModel);
        }

        // Adds company name, inspector, and date range to the top-right
        private static void CreateRightHeader(Section section, PdfHeaderModel headerModel)
        {
            var rightFrame = section.Headers.Primary.AddTextFrame();
            rightFrame.Width = "7cm";
            rightFrame.Height = "4cm";
            rightFrame.RelativeVertical = RelativeVertical.Page;
            rightFrame.RelativeHorizontal = RelativeHorizontal.Page;

            var pageWidth = section.PageSetup.PageWidth;
            var frameWidth = Unit.FromCentimeter(6);
            var rightMargin = Unit.FromCentimeter(2.5);

            // ↓ Slightly lower than the left frame to align visually with logo center
            rightFrame.Top = "2.1cm";
            rightFrame.Left = (pageWidth - frameWidth - rightMargin).ToString();

            // Company name aligned to logo center
            var companyParagraph = rightFrame.AddParagraph(headerModel.CompanyName);
            companyParagraph.Format.Font.Bold = true;
            companyParagraph.Format.Font.Size = 14;
            companyParagraph.Format.Alignment = ParagraphAlignment.Right;
            companyParagraph.Format.SpaceAfter = "0.35cm";

            // Inspector name aligned with report title
            var inspectorParagraph = rightFrame.AddParagraph(headerModel.InspectorName);
            inspectorParagraph.Format.Font.Size = 12;
            inspectorParagraph.Format.Alignment = ParagraphAlignment.Right;
            inspectorParagraph.Format.SpaceAfter = "0.35cm";

            // Date range aligned with generated date
            var dateRangeParagraph = rightFrame.AddParagraph($"Data from {headerModel.DateRange}");
            dateRangeParagraph.Format.Font.Size = 12;
            dateRangeParagraph.Format.Alignment = ParagraphAlignment.Right;
        }


        private static void CreateLeftHeader(Section section, PdfHeaderModel headerModel)
        {
            var leftFrame = section.Headers.Primary.AddTextFrame();
            leftFrame.Width = "10cm";
            leftFrame.Height = "4cm";
            leftFrame.RelativeVertical = RelativeVertical.Page;
            leftFrame.RelativeHorizontal = RelativeHorizontal.Page;

            // Align top with rightFrame
            leftFrame.Top = "1.5cm";
            leftFrame.Left = "1.5cm";

            string logoPath = Path.Combine(Directory.GetCurrentDirectory(), headerModel.LogoPath);
            if (File.Exists(logoPath))
            {
                var logo = leftFrame.AddImage(logoPath);
                logo.Width = "4cm";
                logo.LockAspectRatio = true;
            }
            else
            {
                leftFrame.AddParagraph("Logo Not Found");
            }

            // Report title aligned with inspector name
            var titleParagraph = leftFrame.AddParagraph(headerModel.Title);
            titleParagraph.Format.Font.Size = 16;
            titleParagraph.Format.Font.Bold = true;
            titleParagraph.Format.SpaceBefore = "0.3cm";
            titleParagraph.Format.SpaceAfter = "0.3cm";
            titleParagraph.Format.Alignment = ParagraphAlignment.Left;

            // Generated date aligned with date range
            var genLine = leftFrame.AddParagraph();
            genLine.Format.Font.Size = 12;
            genLine.Format.Alignment = ParagraphAlignment.Left;

            var boldText = genLine.AddFormattedText("Generated on ", TextFormat.Bold);
            genLine.AddText(headerModel.GeneratedDate);
        }

    }
}
